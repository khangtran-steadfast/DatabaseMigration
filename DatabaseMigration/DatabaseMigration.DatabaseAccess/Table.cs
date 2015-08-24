using DatabaseMigration.Infrastructure.Configurations;
using DatabaseMigration.Infrastructure.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseMigration.DatabaseAccess
{
    public class Table
    {
        #region Fields

        // Dump query to get schema only
        private const string DUMP_QUERY = "SELECT * FROM {0} WHERE 0=1";
        private Database _database;
        private string _name;
        private string _fullName;
        private List<Field> _fields;

        #endregion

        #region Properties
        
        public string Name
        {
            get { return _name; }
        }
        
        public string FullName
        {
            get { return _fullName; }
        }

        public bool IsMapped { get; set; }
        
        public List<Field> Fields
        {
            get { return _fields ?? (_fields = new List<Field>()); }
        }

        #endregion

        public Table(string catalog, string schema, string name, Database database, SqlConnection connection)
        {
            _database = database;
            _name = name;
            _fullName = string.Format("[{0}].{1}.[{2}]", catalog, schema, name);
            Initialize(connection);
        }

        public Field GetField(string fieldName)
        {
            try
            {
                var table = Fields.Single(t => t.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase));
                return table;
            }
            catch (InvalidOperationException)
            {
                throw new MigrationException(MigrationExceptionCodes.DATABASE_ERROR_FIELD_NOT_FOUND, fieldName);
            }
        }

        public Field GetPrimaryKey()
        {
            try
            {
                return _fields.First(f => f.Type.HasFlag(FieldType.PrimaryKey));
            }
            catch(InvalidOperationException)
            {
                throw new MigrationException(MigrationExceptionCodes.DATABASE_ERROR_PK_NOT_FOUND);
            }
        }

        public List<Reference> GetReferences()
        {
            var result = Fields.Where(f => f.Type.HasFlag(FieldType.ForeignKey)).Select(f => f.Reference).ToList();
            return result;
        }

        public Field GetCorrespondingField(string destinationFieldName, List<FieldMappingConfiguration> mappingConfigs)
        {
            Field sourceField;
            if (mappingConfigs != null)
            {
                var mappingConfig = mappingConfigs.SingleOrDefault(m => m.DestinationFieldName.Equals(destinationFieldName, StringComparison.InvariantCultureIgnoreCase));
                if (mappingConfig != null)
                {
                    string mapSourceFieldName = mappingConfig.SourceFieldName != null ? mappingConfig.SourceFieldName : destinationFieldName;
                    sourceField = GetField(mapSourceFieldName);
                }
                else
                {
                    string mapSourceFieldName = destinationFieldName;
                    sourceField = GetField(mapSourceFieldName);
                }
            }
            else
            {
                string mapSourceFieldName = destinationFieldName;
                sourceField = GetField(mapSourceFieldName);
            }

            return sourceField;
        }

        private void Initialize(SqlConnection connection)
        {
            ReadFieldsSchema(connection);
        }

        private void ReadFieldsSchema(SqlConnection connection)
        {
            string query = string.Format(DUMP_QUERY, _name);
            using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
            {
                List<string> primaryKeys = ReadPrimaryKeys(connection);
                List<string> uniqueFields = ReadUniqueFields(connection);

                DataTable table = new DataTable(_name);
                DataColumnCollection columns = adapter.FillSchema(table, SchemaType.Mapped).Columns;
                foreach (DataColumn column in columns)
                {
                    Field field = new Field(column.ColumnName, column.DataType.Name);
                    if (primaryKeys.Contains(field.Name))
                    {
                        field.Type |= FieldType.PrimaryKey;
                        if (column.AutoIncrement)
                            field.Type |= FieldType.Identity;
                    }

                    if(uniqueFields.Contains(field.Name))
                    {
                        field.Type |= FieldType.Unique;
                    }

                    if(column.AllowDBNull)
                    {
                        field.AllowNull = true;
                    }

                    Fields.Add(field);
                }
            }
        }

        private List<string> ReadPrimaryKeys(SqlConnection connection)
        {
            var result = new List<string>();

            string getPKTemplate = @"SELECT column_name
                                    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                                    WHERE OBJECTPROPERTY(OBJECT_ID(constraint_name), 'IsPrimaryKey') = 1
                                    AND table_name = '{0}'";
            string getPKQuery = string.Format(getPKTemplate, _name);
            using (SqlDataAdapter adapter = new SqlDataAdapter(getPKQuery, connection))
            {
                DataTable table = new DataTable(_name);
                adapter.Fill(table);
                foreach (DataRow row in table.Rows)
                {
                    var fieldName = row["column_name"].ToString();
                    result.Add(fieldName);
                }
            }

            return result;
        }

        private List<string> ReadUniqueFields(SqlConnection connection)
        {
            List<string> result = new List<string>();

            string getUniqueFieldsTemplate = @"SELECT  COLUMN_NAME
                                            FROM    information_schema.table_constraints TC
                                                    INNER JOIN information_schema.constraint_column_usage CC
                                                        ON TC.Constraint_Name = CC.Constraint_Name
                                            WHERE   TC.constraint_type = 'Unique' AND CC.TABLE_NAME = '{0}'
                                            ORDER BY TC.Constraint_Name";

            string getUniqueFieldsQuery = string.Format(getUniqueFieldsTemplate, _name);
            using (SqlDataAdapter adapter = new SqlDataAdapter(getUniqueFieldsQuery, connection))
            {
                DataTable table = new DataTable();
                adapter.Fill(table);
                foreach (DataRow row in table.Rows)
                {
                    var fieldName = row["COLUMN_NAME"].ToString();
                    result.Add(fieldName);
                }
            }

            return result;
        }

        #region Detect circle reference

        public List<string> GetCircleReferences()
        {
            return GetCircleReferences(new List<string>());
        }

        private List<string> GetCircleReferences(List<string> tracking)
        {
            //bool result = false;
            var result = new List<string>();

            // If we can not detect circle reference in this level
            // Try to access deeper level (using recursion algorithm)
            List<Reference> references = GetReferences();
            List<string> listTableHasCheck = new List<string>();
            listTableHasCheck.Add(Name);
            foreach (Reference reference in references)
            {
                var response = GetCircleReferences(reference, listTableHasCheck, tracking);
                if (response)
                {
                    // Add tracking if needed
                    //if (tracking != null)
                    //{
                    //    tracking.Add(Name);
                    //}
                    tracking.ForEach(t => result.Add(t));
                    break;
                }
            }

            return result;
        }

        private bool GetCircleReferences(Reference reference, List<string> listTableHasCheck, List<string> tracking)
        {
            // Initialize result
            bool result = false;

            // Check current reference
            if (reference.ReferenceTableName.Equals(Name))
            {
                if (tracking != null)
                {
                    tracking.Add(Name);
                }
                result = true;
            }
            else
            {
                // Try to get referenced table
                // And go deeper (using recursion algorithm)
                Table referencedTable = _database.GetTable(reference.ReferenceTableName);
                List<Reference> listReference = referencedTable.GetReferences();
                listTableHasCheck.Add(referencedTable.Name);
                foreach (Reference childReference in listReference)
                {
                    // Check
                    if (childReference.ReferenceTableName.Equals(Name))
                    {
                        // Add to tracking list if needed
                        if (tracking != null)
                        {
                            tracking.Add(referencedTable.Name);
                        }
                        result = true;
                        break;
                    }

                    // If referenced table is checked
                    // We don't need to go further
                    if (listTableHasCheck.Contains(childReference.ReferenceTableName))
                    {
                        continue;
                    }

                    // Else try to go deeper
                    var response = this.GetCircleReferences(childReference, listTableHasCheck, tracking);
                    if (response)
                    {
                        // Add to tracking list if needed
                        //if (tracking != null)
                        //{
                        //    tracking.Add(referencedTable.Name);
                        //}
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
