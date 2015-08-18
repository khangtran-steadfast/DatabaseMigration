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

namespace DatabaseMigration.Manager
{
    public class MigrationManager
    {
        private int _processedTablesCount;
        private MigrationOptions _options;

        private SourceDatabase _sourceDatabase { get; set; }
        private DestinationDatabase _destinationDatabase { get; set; }

        public MigrationManager(string sourceConnectionString, string destinationConnectionString, MigrationOptions options)
        {
            _options = options;

            _sourceDatabase = new SourceDatabase(sourceConnectionString);
            _sourceDatabase.Initialize();

            _destinationDatabase = new DestinationDatabase(destinationConnectionString);
            _destinationDatabase.Initialize();

            _sourceDatabase.LearnDestinationDatabaseReference(_destinationDatabase, options.ExplicitTableMappings);
        }

        public void GenerateMigrationScripts()
        {
            bool hasExplicitMappings = _options != null && _options.ExplicitTableMappings != null;
            int count = 0;
            int tablesCount = _destinationDatabase.Tables.Count;
            string outputPath = ConfigurationManager.AppSettings["SQLOutputFolder"];
            outputPath = Path.Combine(outputPath, DateTime.Now.ToString("ddMMyyyy"));
            List<string> scriptNames = new List<string>();
            while (_processedTablesCount < tablesCount)
            {
                Table destinationTable = null;
                Table sourceTable = null;
                string mapSourceTableName = null;
                try
                {
                    // Get next table to migrate
                    destinationTable = GetNextTableCanMap();
                    
                    // Check explicit mapping for source table - destination table
                    TableMappingDefinition mappingDefinition;
                    if (hasExplicitMappings)
                    {
                        var mappingConfig = _options.ExplicitTableMappings.SingleOrDefault(m => m.DestinationTableName.Equals(destinationTable.Name, StringComparison.InvariantCultureIgnoreCase));
                        if (mappingConfig != null)
                        {
                            mapSourceTableName = mappingConfig.SourceTableName != null ? mappingConfig.SourceTableName : destinationTable.Name;
                            sourceTable = _sourceDatabase.GetTable(mapSourceTableName);
                            mappingDefinition = new TableMappingDefinition(sourceTable, destinationTable, mappingConfig.FieldMappings);
                            mappingDefinition.IsIdentityInsert = mappingConfig.IsIdentityInsert;
                        }
                        else
                        {
                            mapSourceTableName = destinationTable.Name;
                            sourceTable = _sourceDatabase.GetTable(mapSourceTableName);
                            mappingDefinition = new TableMappingDefinition(sourceTable, destinationTable);
                        }
                    }
                    else
                    {
                        mapSourceTableName = destinationTable.Name;
                        sourceTable = _sourceDatabase.GetTable(destinationTable.Name);
                        mappingDefinition = new TableMappingDefinition(sourceTable, destinationTable);
                    }

                    // Generate script 
                    var scriptGenerator = new TableScriptGenerator(_sourceDatabase, mappingDefinition);
                    var script = scriptGenerator.GenerateScript();
                    var fileName = string.Format("{0}.{1}-{2}.sql", count++, sourceTable.Name, destinationTable.Name);
                    SaveToFile(outputPath, fileName, script);
                    scriptNames.Add(fileName);

                    destinationTable.IsMapped = true;
                    _processedTablesCount++;
                }
                catch (MigrationException ex)
                {
                    if (ex.ErrorCode == MigrationExceptionCodes.DATABASE_ERROR_TABLE_NOT_FOUND)
                    {
                        destinationTable.IsMapped = true;

                        // TODO: write log
                        Console.WriteLine(mapSourceTableName + " -> " + destinationTable.Name);
                    }
                }
            }

            // Generate script clear temp database
            var clearScript = string.Format(SqlScriptTemplates.TRUNCATE_TABLE, "[TempDatabase].dbo.[TrackingRecords]");
            var clearFileName = string.Format("{0}.Clear.sql", count);
            SaveToFile(outputPath, clearFileName, clearScript);
            scriptNames.Add(clearFileName);

            // Generate bat file
            GenerateBatFile(scriptNames, outputPath);
        }

        private void GenerateBatFile(List<string> scriptNames, string outputPath)
        {
            StringBuilder batBuilder = new StringBuilder();

            scriptNames.ForEach(s =>
            {
                string scriptPath = Path.Combine(Path.GetFullPath(outputPath), s);
                batBuilder.AppendLine(string.Format(BatTemplates.ECHO, s));
                batBuilder.AppendLine(BatTemplates.EXECUTE_SQL.Inject(new
                {
                    ServerName = _options.ServerName,
                    InstanceName = _options.InstanceName,
                    ScriptPath = scriptPath
                }));
            });

            string content = string.Format(BatTemplates.BAT, batBuilder.ToString());
            SaveToFile(outputPath, "RunMigration.bat", content);
        }

        private void SaveToFile(string location, string fileName, string content)
        {
            string filePath = Path.Combine(location, fileName);

            FileInfo file = new FileInfo(filePath);
            file.Directory.Create();
            File.WriteAllText(file.FullName, content);
        }

        private Table GetNextTableCanMap()
        {
            var result = _destinationDatabase.Tables.FirstOrDefault(t => CanMap(t));
            return result;
        }

        private bool CanMap(Table table)
        {
            bool result = true;
            if (table.IsMapped)
            {
                result = false;
            }
            else
            {
                var referenceFields = table.Fields.Where(f => f.Type.HasFlag(FieldType.ForeignKey)).ToList();
                foreach (Field field in referenceFields)
                {
                    Table referenceTable = _destinationDatabase.GetTable(field.Reference.ReferenceTableName);
                    if (referenceTable.IsMapped == false)
                    {
                        result = false;
                        break;
                    }
                }
            }

            return result;
        }
    }
}
