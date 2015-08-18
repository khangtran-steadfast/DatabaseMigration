using DatabaseMigration.DatabaseAccess;
using DatabaseMigration.Manager.Constants;
using DatabaseMigration.Manager.MappingDefinition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StringInject;
using DatabaseMigration.Infrastructure.Enums;
using DatabaseMigration.Infrastructure.Helpers;

namespace DatabaseMigration.Manager.ScriptGenerator
{
    class TableScriptGenerator
    {
        private SourceDatabase _sourceDatabase;
        private TableMappingDefinition _definition;

        public TableScriptGenerator(SourceDatabase sourceDatabase, TableMappingDefinition definition)
        {
            _sourceDatabase = sourceDatabase;
            _definition = definition;
        }

        public string GenerateScript()
        {
            string result = "";

            List<FieldMappingDefinition> blobMappings = GetBlobMappings();
            bool hasBlobs = blobMappings.Any();
            string blobsScript = "";
            if (hasBlobs)
            {
                blobsScript = HandleBlobFields(blobMappings) + NewLines(2);
            }

            result = blobsScript + GenerateMergeScript(_definition.IsIdentityInsert);

            if (hasBlobs)
            {
                string truncateBlobPointerTableScript = string.Format(SqlScriptTemplates.TRUNCATE_TABLE, "[TempDatabase].dbo.[BlobPointers]");
                result += NewLines(2) + truncateBlobPointerTableScript;
            }

            return result;
        }

        /// <summary>
        /// Saves Blob to file -> returns Blob pointer -> generates script insert for blobs as PK|BlobPointer to keep track
        /// After migrating data for a table (which contains Blob), will need this data to set Blob pointer 
        /// </summary>
        /// <param name="blobMappings">The BLOB mappings.</param>
        /// <returns></returns>
        private string HandleBlobFields(List<FieldMappingDefinition> blobMappings)
        {
            string insertScript = string.Format(SqlScriptTemplates.INSERT, "[TempDatabase].dbo.[BlobPointers]", "([PKValue], [BlobPointer])");
            StringBuilder valuesScriptBuilder = new StringBuilder();
            blobMappings.ForEach(m =>
            {
                var blobs = _sourceDatabase.GetBlobs(_definition.SourceTable.Name, m.SourceField.Name);
                blobs.ForEach(b =>
                {
                    byte[] data = b.Value;
                    string blobPointer = BlobConverter.ConvertToFile("XXX", "XXX", data);

                    valuesScriptBuilder.Append(Environment.NewLine + string.Format("('{0}','{1}')", b.Key, blobPointer) + ",");
                });
            });

            string valuesScript = valuesScriptBuilder.ToString().Trim(',');
            return insertScript + Environment.NewLine + string.Format(SqlScriptTemplates.INSERT_VALUES, valuesScript);
        }

        /// <summary>
        /// Generates script for inserting data from table to table using SqlScriptTemplates.MERGE template
        /// </summary>
        /// <param name="isIdentityInsert">if set to <c>true</c> [is identity insert].</param>
        /// <returns></returns>
        private string GenerateMergeScript(bool isIdentityInsert)
        {
            Table sourceTable = _definition.SourceTable;
            Table destinationTable = _definition.DestinationTable;
            Field sourcePK = sourceTable.Fields.FirstOrDefault(x => x.Type.HasFlag(FieldType.PrimaryKey));
            Field destinationPK = destinationTable.Fields.FirstOrDefault(x => x.Type.HasFlag(FieldType.PrimaryKey));
            bool hasPK = sourcePK != null && destinationPK != null;

            // Generate template's parts
            string sourceSelectFields;
            string sourceInsertFields;
            string targetInsertFields;
            string recordComparison;
            FieldScriptGenerator fieldScriptGenerator = new FieldScriptGenerator(_definition.FieldMappingDefinitions);
            fieldScriptGenerator.GeneratePartsForMergeTemplate(out targetInsertFields, out sourceInsertFields, out sourceSelectFields, out recordComparison, isIdentityInsert, hasPK);

            // Generate Merge SQL
            string mergeSql = "";
            if(hasPK)
            {
                mergeSql = SqlScriptTemplates.MERGE_HAS_PK.Inject(new
                {
                    TargetTableFullName = destinationTable.FullName,
                    SourceTableFullName = sourceTable.FullName,
                    SourceTableName = sourceTable.Name,
                    TargetPKName = destinationPK.Name,
                    SourcePKName = sourcePK.Name,
                    SourcePKDataType = sourcePK.DataType,
                    SourceSelectFields = sourceSelectFields,
                    SourceInsertFields = sourceInsertFields,
                    TargetInsertFields = targetInsertFields,
                    RecordComparison = recordComparison
                });
            }
            else
            {
                mergeSql = SqlScriptTemplates.MERGE_NO_PK.Inject(new
                {
                    TargetTableFullName = destinationTable.FullName,
                    SourceTableFullName = sourceTable.FullName,
                    SourceSelectFields = sourceSelectFields,
                    SourceInsertFields = sourceInsertFields,
                    TargetInsertFields = targetInsertFields,
                    RecordComparison = recordComparison
                });
            }

            // Check Identity Insert
            string result = "";
            if (isIdentityInsert)
            {
                result += string.Format(SqlScriptTemplates.IDENTITY_INSERT_ON, destinationTable.FullName) + Environment.NewLine;
                result += mergeSql + Environment.NewLine;
                result += string.Format(SqlScriptTemplates.IDENTITY_INSERT_OFF, destinationTable.FullName);
            }
            else
            {
                result += mergeSql;
            }
            
            return result;
        }

        private List<FieldMappingDefinition> GetBlobMappings()
        {
            List<FieldMappingDefinition> result = _definition.FieldMappingDefinitions.Where(d => d.Type == FieldMappingType.BlobToBlobPointer).ToList();
            return result;
        }

        private string NewLines(int count)
        {
            string lines = "";

            for (int i = 0; i < count; i++)
            {
                lines += Environment.NewLine;
            }

            return lines;
        }
    }
}
