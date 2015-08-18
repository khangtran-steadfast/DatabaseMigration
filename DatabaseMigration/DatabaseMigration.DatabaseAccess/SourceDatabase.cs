using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StringInject;
using DatabaseMigration.Infrastructure.Configurations;
using DatabaseMigration.Infrastructure.Exceptions;

namespace DatabaseMigration.DatabaseAccess
{
    public class SourceDatabase : Database
    {
        public SourceDatabase(string connectionString) : base(connectionString)
        {

        }

        public List<KeyValuePair<int, byte[]>> GetBlobs(string tableName, string fieldName)
        {
            List<KeyValuePair<int, byte[]>> result = new List<KeyValuePair<int, byte[]>>();

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                Table table = GetTable(tableName);
                Field pkField = table.GetPrimaryKey();
                Field blobField = table.GetField(fieldName);

                string query = "SELECT {PrimaryKey}, {Blob} FROM {Table}".Inject(new { PrimaryKey = pkField.Name, Blob = blobField.Name, Table = table.FullName });
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                {
                    DataTable dataTable = new DataTable(table.Name);
                    adapter.Fill(dataTable);
                    foreach (DataRow row in dataTable.Rows)
                    {
                        int id = (int)row[pkField.Name];
                        byte[] blob = (byte[])row[blobField.Name];
                        result.Add(new KeyValuePair<int, byte[]>(id, blob));
                    }
                }
            }

            return result;
        }

        public void LearnDestinationDatabaseReference(DestinationDatabase destinationDatabase, List<TableMappingConfiguration> tableMappingConfigs)
        {
            destinationDatabase.Tables.Where(t => t.Fields.Any(f => f.Type.HasFlag(FieldType.ForeignKey))).ToList().ForEach(t =>
            {
                try
                {
                    List<FieldMappingConfiguration> fieldMappingConfigs;
                    Table sourceTable = GetCorrespondingTable(t.Name, tableMappingConfigs, out fieldMappingConfigs);
                    t.Fields.ForEach(f =>
                    {
                        if (f.Type == FieldType.ForeignKey)
                        {
                            Reference destinationFieldReference = f.Reference;
                            Field sourceField = sourceTable.GetCorrespondingField(f.Name, fieldMappingConfigs);
                            if (sourceField.Reference == null)
                            {
                                List<FieldMappingConfiguration> temp;
                                Table referenceTable = GetCorrespondingTable(destinationFieldReference.ReferenceTableName, tableMappingConfigs, out temp);
                                Field referenceField = referenceTable.GetCorrespondingField(destinationFieldReference.ReferenceFieldName, temp);
                                sourceField.Reference = new Reference(null, sourceTable.Name, sourceField.Name, referenceTable.Name, referenceField.Name);
                                sourceField.Type = FieldType.ForeignKey;
                            }
                        }
                    });
                }
                catch (MigrationException ex)
                {
                    // TODO: Write log
                    if(ex.ErrorCode == MigrationExceptionCodes.DATABASE_ERROR_FIELD_NOT_FOUND)
                    {

                    }
                    else if(ex.ErrorCode == MigrationExceptionCodes.DATABASE_ERROR_TABLE_NOT_FOUND)
                    {

                    }
                }
            });
        }

        private Table GetCorrespondingTable(string destinationTableName, List<TableMappingConfiguration> mappingConfigs, out List<FieldMappingConfiguration> fieldMappingConfigs)
        {
            fieldMappingConfigs = null;
            Table sourceTable;
            if (mappingConfigs != null)
            {
                var mappingConfig = mappingConfigs.SingleOrDefault(m => m.DestinationTableName.Equals(destinationTableName, StringComparison.InvariantCultureIgnoreCase));
                if (mappingConfig != null)
                {
                    string mapSourceTableName = mappingConfig.SourceTableName != null ? mappingConfig.SourceTableName : destinationTableName;
                    sourceTable = GetTable(mapSourceTableName);
                    fieldMappingConfigs = mappingConfig.FieldMappings;
                }
                else
                {
                    string mapSourceTableName = destinationTableName;
                    sourceTable = GetTable(mapSourceTableName);
                }
            }
            else
            {
                string mapSourceTableName = destinationTableName;
                sourceTable = GetTable(mapSourceTableName);
            }

            return sourceTable;
        }
    }
}
