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

namespace DatabaseMigration.Manager.ScriptGenerator
{
    public class FieldScriptGenerator
    {
        private List<FieldMappingDefinition> _definitions;

        public FieldScriptGenerator(List<FieldMappingDefinition> definitions)
        {
            _definitions = definitions;
        }

        /// <summary>
        /// Generates neccessary parts for SqlScriptTemplates.MERGE_HAS_PK template
        /// </summary>
        /// <param name="isIdentityInsert">if set to <c>true</c> [is identity insert].</param>
        public void GeneratePartsForMergeTemplate(out string targetInsertFields, out string sourceInsertFields, out string sourceSelectFields, out string recordComparison, bool isIdentityInsert, bool hasPK)
        {
            List<string> targetInsertExpressions = new List<string>();
            List<string> sourceInsertExpressions = new List<string>();
            List<string> sourceSelectExpressions = new List<string> { "*" };
            List<string> recordComparisonExpressions = new List<string>();
            foreach (FieldMappingDefinition definition in _definitions)
            {
                /*
                 * Identity Insert => include all fields
                 *                 => else exclude identity fields
                 * */
                if (isIdentityInsert)
                {
                    Generate(definition, targetInsertExpressions, sourceInsertExpressions, sourceSelectExpressions, hasPK);
                }
                else
                {
                    if (!definition.DestinationField.Type.HasFlag(FieldType.Identity))
                    {
                        Generate(definition, targetInsertExpressions, sourceInsertExpressions, sourceSelectExpressions, hasPK);
                    }
                }

                // RecordComparison
                Field sourceField = definition.SourceField;
                Field destinationField = definition.DestinationField;
                if (hasPK)
                {
                    if (sourceField.Type.HasFlag(FieldType.PrimaryKey) && destinationField.Type.HasFlag(FieldType.PrimaryKey))
                    {
                        recordComparisonExpressions.Add(SqlScriptTemplates.MERGE_RECORD_COMPARISON.Inject(new
                        {
                            TargetPKName = destinationField.Name,
                            SourcePKName = sourceField.Name
                        }));
                    }
                }
                else
                {
                    recordComparisonExpressions.Add(SqlScriptTemplates.MERGE_RECORD_COMPARISON.Inject(new
                    {
                        TargetPKName = destinationField.Name,
                        SourcePKName = sourceField.Name
                    }));
                }
            }

            targetInsertFields = targetInsertExpressions.Aggregate((x, y) => x + "," + y);
            sourceInsertFields = sourceInsertExpressions.Aggregate((x, y) => x + "," + y);
            sourceSelectFields = sourceSelectExpressions.Aggregate((x, y) => x + "," + Environment.NewLine + y);
            recordComparison = recordComparisonExpressions.Aggregate((x, y) => x + " AND " + y);
        }

        private void Generate(FieldMappingDefinition definition, List<string> targetInsertExpressions, List<string> sourceInsertExpressions, List<string> sourceSelectExpressions, bool hasPK)
        {
            Field sourceField = definition.SourceField;
            Field destinationField = definition.DestinationField;

            // SourceSelectFields, SourceInsertFields
            if (destinationField.Type.HasFlag(FieldType.ForeignKey))
            {
                sourceSelectExpressions.Add(SqlScriptTemplates.FK_SELECT.Inject(new
                {
                    FKTable = sourceField.Reference.ReferenceTableName,
                    SourceFKName = sourceField.Name
                }));

                sourceInsertExpressions.Add(string.Format(SqlScriptTemplates.NEW_FIELD, sourceField.Name));
            }
            else if (definition.Type == FieldMappingType.BlobToBlobPointer)
            {
                sourceSelectExpressions.Add(SqlScriptTemplates.BLOB_SELECT.Inject(new
                {
                    SourceBlobFieldName = sourceField.Name,
                    SourcePKName = GetSourcePrimaryKey().Name
                }));

                sourceInsertExpressions.Add(string.Format(SqlScriptTemplates.NEW_FIELD, sourceField.Name));
            }
            else
            {
                sourceInsertExpressions.Add(string.Format(SqlScriptTemplates.FIELD, sourceField.Name));
            }

            // TargetInsertFields
            targetInsertExpressions.Add(string.Format(SqlScriptTemplates.FIELD, destinationField.Name));
        }

        private Field GetSourcePrimaryKey()
        {
            return _definitions.Select(d => d.SourceField).First(f => f.Type.HasFlag(FieldType.PrimaryKey));
        }
    }
}
