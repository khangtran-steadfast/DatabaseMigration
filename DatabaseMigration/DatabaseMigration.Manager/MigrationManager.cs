using DatabaseMigration.DatabaseAccess;
using DatabaseMigration.Infrastructure.Configurations;
using DatabaseMigration.Manager.MappingDefinition;
using DatabaseMigration.Manager.ScriptGenerator;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StringInject;
using DatabaseMigration.Manager.Constants;
using DatabaseMigration.Infrastructure.Exceptions;
using DatabaseMigration.Manager.Helpers;

namespace DatabaseMigration.Manager
{
    public class MigrationManager
    {
        private MigrationOptions _options;
        private SourceDatabase _sourceDatabase;
        private DestinationDatabase _destinationDatabase;
        private List<TableMappingDefinition> _tableMappingDefinitions = new List<TableMappingDefinition>();

        /// <summary>
        /// List tables to ignore references to
        /// </summary>
        /// <example>
        /// B: {A} -> Ignore references from B to A
        /// </example>
        private Dictionary<string, List<Reference>> _ignoreCircleReferences = new Dictionary<string, List<Reference>>();

        /// <summary>
        /// List tables that need to update for circle references when do migration
        /// </summary>
        /// <example>
        /// A: {B} -> Update B reference when do A migration
        /// </example>
        private Dictionary<string, List<Reference>> _tablesToUpdateCircleReferences = new Dictionary<string, List<Reference>>();

        public MigrationManager(string sourceConnectionString, string destinationConnectionString, MigrationOptions options)
        {
            _options = options;

            _sourceDatabase = new SourceDatabase(sourceConnectionString);
            _sourceDatabase.Initialize();

            _destinationDatabase = new DestinationDatabase(destinationConnectionString);
            _destinationDatabase.Initialize();

            _sourceDatabase.LearnDestinationDatabaseReference(_destinationDatabase, options.ExplicitTableMappings);

            ConfigIgnoreCircleReferences();
        }

        public void GenerateMigrationScripts()
        {
            int count = 0;
            string outputPath = ConfigurationManager.AppSettings["SQLOutputFolder"];
            outputPath = Path.Combine(outputPath, DateTime.Now.ToString("ddMMyyyy"));
            List<string> scriptNames = new List<string>();
            var temp = new List<string>();
            while (count < _destinationDatabase.Tables.Count)
            {
                DestinationTable destinationTable = null;
                SourceTable sourceTable = null;
                try
                {
                    // Get next table to migrate
                    destinationTable = GetNextTableCanMap();
                    if (destinationTable == null)
                        break;

                    Console.WriteLine("Processing " + destinationTable.Name);

                    // Check explicit mapping for source table - destination table
                    TableMappingDefinition mappingDefinition;
                    TableMappingConfiguration mappingConfig = GetTableMappingConfig(destinationTable.Name);
                    sourceTable = GetSourceTable(destinationTable.Name, mappingConfig);
                    if(mappingConfig != null)
                    {
                        mappingDefinition = new TableMappingDefinition(sourceTable, destinationTable, mappingConfig.FieldMappings);
                        mappingDefinition.IsIdentityInsert = mappingConfig.IsIdentityInsert;
                    }
                    else
                    {
                        mappingDefinition = new TableMappingDefinition(sourceTable, destinationTable);
                    }

                    // Retain mapping definition for later use
                    _tableMappingDefinitions.Add(mappingDefinition);

                    // Check circle references needed to update
                    CheckCircleReferences(destinationTable, mappingDefinition);

                    // Generate script 
                    var scriptGenerator = new TableScriptGenerator(_sourceDatabase, mappingDefinition);
                    var script = scriptGenerator.GenerateScript();
                    var fileName = string.Format("{0}.{1}-{2}.sql", count++, sourceTable.Name, destinationTable.Name);
                    Utils.SaveToFile(outputPath, fileName, script);
                    scriptNames.Add(fileName);
                    temp.Add(destinationTable.Name);

                    destinationTable.IsMapped = true;
                }
                catch (MigrationException ex)
                {
                    if (ex.ErrorCode == MigrationExceptionCodes.DATABASE_ERROR_TABLE_NOT_FOUND)
                    {
                        destinationTable.IsMapped = true;

                        // TODO: write log
                        Console.WriteLine(destinationTable.Name + " not mapped");
                    }
                }
            }

            // Generate script clear temp database
            var clearScript = string.Format(SqlScriptTemplates.TRUNCATE_TABLE, "[TempDatabase].dbo.[TrackingRecords]");
            var clearFileName = string.Format("{0}.Clear.sql", count);
            Utils.SaveToFile(outputPath, clearFileName, clearScript);
            scriptNames.Add(clearFileName);

            // Generate bat file
            string batScript = BatGenerator.GenerateSqlExecuteScript(scriptNames, _options.ServerName, _options.InstanceName, outputPath);
            Utils.SaveToFile(outputPath, "RunMigration.bat", batScript);
        }

        private TableMappingConfiguration GetTableMappingConfig(string tableName)
        {
            return (_options != null && _options.ExplicitTableMappings != null)
                    ? _options.ExplicitTableMappings.SingleOrDefault(m => m.DestinationTableName.Equals(tableName, StringComparison.InvariantCultureIgnoreCase))
                    : null;
        }

        private SourceTable GetSourceTable(string destinationTableName, TableMappingConfiguration mappingConfig)
        {
            SourceTable sourceTable = null;
            if (mappingConfig != null)
            {
                string mapSourceTableName = mappingConfig.SourceTableName != null ? mappingConfig.SourceTableName : destinationTableName;
                sourceTable = _sourceDatabase.GetTable(mapSourceTableName);
            }
            else
            {
                sourceTable = _sourceDatabase.GetTable(destinationTableName);
            }


            return sourceTable;
        }

        #region Circle references

        private void ConfigIgnoreCircleReferences()
        {
            //IngoreReferenceWhenGetMapOrder("branches", "branches");
            //IngoreReferenceWhenGetMapOrder("general_insurance", "workbooks");
            //IngoreReferenceWhenGetMapOrder("general_insurance", "policies");
            ConfigIngoreReference(new Reference(null, "branches", "bra_parent_branch", "branches", "bra_id"));
            ConfigIngoreReference(new Reference(null, "general_insurance", "genins_current_workbook", "workbooks", "wor_id"));
            ConfigIngoreReference(new Reference(null, "general_insurance", "genins_current_policy", "policies", "pol_id"));

            //ConfigIngoreReference(new Reference(null, "Table_B", "Table_A_Id", "Table_A", "Id"));
        }

        private void ConfigIngoreReference(Reference reference)
        {
            if (!_ignoreCircleReferences.ContainsKey(reference.OriginTableName))
            {
                _ignoreCircleReferences.Add(reference.OriginTableName, new List<Reference>());
            }
            _ignoreCircleReferences[reference.OriginTableName].Add(reference);

            if (!_tablesToUpdateCircleReferences.ContainsKey(reference.ReferenceTableName))
            {
                _tablesToUpdateCircleReferences.Add(reference.ReferenceTableName, new List<Reference>());
            }
            _tablesToUpdateCircleReferences[reference.ReferenceTableName].Add(reference);
        }

        private void CheckCircleReferences(Table table, TableMappingDefinition mappingDefinition)
        {
            if (_tablesToUpdateCircleReferences.ContainsKey(table.Name))
            {
                _tablesToUpdateCircleReferences[table.Name].ForEach(r =>
                {
                    TableMappingDefinition temp = _tableMappingDefinitions.Single(d => d.DestinationTable.Name.Equals(r.OriginTableName));
                    FieldMappingDefinition fieldMappingDefinition = temp.FieldMappingDefinitions.SingleOrDefault(f => f.DestinationField.Name.Equals(r.OriginFieldName));
                    if(fieldMappingDefinition != null)
                    {
                        SourceTable sourceTable = temp.SourceTable;
                        Field sourceField = fieldMappingDefinition.SourceField;
                        DestinationTable destinationTable = temp.DestinationTable;
                        Field destinationField = fieldMappingDefinition.DestinationField;
                        FieldMappingInfo info = new FieldMappingInfo
                        {
                            DestinationField = destinationField,
                            DestinationTable = destinationTable,
                            SourceField = sourceField,
                            SourceTable = sourceTable
                        };

                        mappingDefinition.CircleReferences.Add(info);
                    }
                });
            }
        }

        #endregion

        #region Mapping Order

        private DestinationTable GetNextTableCanMap()
        {
            var result = _destinationDatabase.Tables.FirstOrDefault(t => CanMap(t));
            return result;
        }

        private bool CanMap(DestinationTable table)
        {
            bool result = true;
            if (table.IsMapped)
            {
                result = false;
            }
            else
            {
                List<Reference> configIngoreReference = null;
                if (this._ignoreCircleReferences.ContainsKey(table.Name))
                {
                    configIngoreReference = this._ignoreCircleReferences[table.Name];
                }

                var referenceFields = table.Fields.Where(f => f.Type.HasFlag(FieldType.ForeignKey)).ToList();
                foreach (Field field in referenceFields)
                {
                    if (configIngoreReference != null && configIngoreReference.Any(r => r.ReferenceTableName.Equals(field.Reference.ReferenceTableName)))
                    {
                        continue;
                    }

                    DestinationTable referenceTable = _destinationDatabase.GetTable(field.Reference.ReferenceTableName);
                    if (referenceTable.IsMapped == false)
                    {
                        result = false;
                        break;
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
