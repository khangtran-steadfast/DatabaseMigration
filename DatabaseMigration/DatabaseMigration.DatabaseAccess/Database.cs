using DatabaseMigration.Infrastructure.Configurations;
using DatabaseMigration.Infrastructure.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StringInject;

namespace DatabaseMigration.DatabaseAccess
{
    public class Database
    {
        #region Fields

        protected string ConnectionString;
        private string _name;
        private List<Table> _tables;

        #endregion

        #region Properties
        
        public string Name
        {
            get { return _name; }
        }
        
        public List<Table> Tables
        {
            get { return _tables ?? (_tables = new List<Table>()); }
        }

        #endregion

        public Database(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public void Initialize(List<ReferenceConfiguration> referenceConfigurations = null)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                _name = connection.Database;
                ReadTablesSchema(connection);
                PopulateForeignKeys(referenceConfigurations, connection);
            }
        }

        public Table GetTable(string tableName)
        {
            try
            {
                var table = Tables.Single(t => t.Name.Equals(tableName, StringComparison.InvariantCultureIgnoreCase));
                return table;
            }
            catch (InvalidOperationException)
            {
                throw new MigrationException(MigrationExceptionCodes.DATABASE_ERROR_TABLE_NOT_FOUND, tableName);
            }
        }

        private void ReadTablesSchema(SqlConnection connection)
        {
            DataTable tableSchema = connection.GetSchema("Tables");
            foreach (DataRow row in tableSchema.Rows)
            {
                var catalog = row["TABLE_CATALOG"].ToString();
                var schema = row["TABLE_SCHEMA"].ToString();
                var name = row["TABLE_NAME"].ToString();
                var type = row["TABLE_TYPE"].ToString();

                if(type.Equals("BASE TABLE", StringComparison.InvariantCultureIgnoreCase))
                {
                    Table table = new Table(catalog, schema, name, connection);
                    Tables.Add(table);
                }
            }
        }

        private void PopulateForeignKeys(List<ReferenceConfiguration> referenceConfigurations, SqlConnection connection)
        {
            string getForeignKeyQuery = @"SELECT f.name AS ForeignKeyName, OBJECT_NAME(f.parent_object_id) AS TableName,
                                            COL_NAME(fc.parent_object_id, fc.parent_column_id) AS ColumnName,
                                            OBJECT_NAME (f.referenced_object_id) AS ReferenceTableName,
                                            COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS ReferenceColumnName
                                        FROM sys.foreign_keys AS f
                                        INNER JOIN sys.foreign_key_columns AS fc
                                        ON f.OBJECT_ID = fc.constraint_object_id";

            using (SqlDataAdapter adapter = new SqlDataAdapter(getForeignKeyQuery, connection))
            {
                // Get references from schema
                DataTable table = new DataTable();
                adapter.Fill(table);
                foreach(DataRow row in table.Rows)
                {
                    var referenceName = row["ForeignKeyName"].ToString();
                    var tableName = row["TableName"].ToString();
                    var columnName = row["ColumnName"].ToString();
                    var referenceTableName = row["ReferenceTableName"].ToString();
                    var referenceColumnName = row["ReferenceColumnName"].ToString();

                    Tables.ForEach(t =>
                    {
                        t.Fields.ForEach(f =>
                        {
                            if(f.Name.Equals(columnName))
                            {
                                f.Type = FieldType.ForeignKey;
                                f.Reference = new Reference(referenceName, tableName, columnName, referenceTableName, referenceColumnName);
                            }
                        });
                    });
                }

                // Override if exists in configurations
                if(referenceConfigurations != null)
                {
                    referenceConfigurations.ForEach(c =>
                    {
                        var tableName = c.OriginTableName;
                        var columnName = c.OriginFieldName;
                        var referenceTableName = c.ReferenceTableName;
                        var referenceColumnName = c.ReferenceFieldName;

                        Tables.ForEach(t =>
                        {
                            t.Fields.ForEach(f =>
                            {
                                if (f.Name.Equals(columnName))
                                {
                                    f.Type = FieldType.ForeignKey;
                                    f.Reference = new Reference(null, tableName, columnName, referenceTableName, referenceColumnName);
                                }
                            });
                        });
                    });
                }
            }
        }
    }
}
