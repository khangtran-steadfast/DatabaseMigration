using DatabaseMigration.DatabaseAccess;
using DatabaseMigration.Infrastructure.Configurations;
using DatabaseMigration.Infrastructure.Enums;
using DatabaseMigration.Infrastructure.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseMigration.Manager.MappingDefinition
{
    class TableMappingDefinition
    {
        #region Fields

        private SourceTable _sourceTable;
        private DestinationTable _destinationTable;
        private List<FieldMappingDefinition> _fieldMappingDefinitions;
        private List<FieldMappingInfo> _circleReferences;

        #endregion

        #region Properties

        public bool IsIdentityInsert { get; set; }

        public SourceTable SourceTable
        {
            get { return _sourceTable; }
        }

        public DestinationTable DestinationTable
        {
            get { return _destinationTable; }
        }

        public List<FieldMappingDefinition> FieldMappingDefinitions
        {
            get { return _fieldMappingDefinitions ?? (_fieldMappingDefinitions = new List<FieldMappingDefinition>()); }
        }

        public List<FieldMappingInfo> CircleReferences 
        {
            get { return _circleReferences ?? (_circleReferences = new List<FieldMappingInfo>()); }
            set { _circleReferences = value; } 
        }

        #endregion

        public TableMappingDefinition(SourceTable sourceTable, DestinationTable destinationTable, List<FieldMappingConfiguration> explicitMappings = null)
        {
            _sourceTable = sourceTable;
            _destinationTable = destinationTable;
            bool hasExplicitMappings = explicitMappings != null;

            destinationTable.Fields.ForEach(f =>
            {
                try
                {
                    if(hasExplicitMappings)
                    {
                        var mappingConfig = explicitMappings.SingleOrDefault(m => m.DestinationFieldName.Equals(f.Name, StringComparison.InvariantCultureIgnoreCase));
                        if (mappingConfig != null)
                        {
                            var sourceField = sourceTable.GetField(mappingConfig.SourceFieldName);
                            FieldMappingDefinitions.Add(new FieldMappingDefinition(sourceField, f, mappingConfig.Type) { BlobCategory = mappingConfig.BlobCategory, ForceValue = mappingConfig.ForceValue });
                        }
                        else
                        {
                            var sourceField = sourceTable.GetField(f.Name);
                            FieldMappingDefinitions.Add(new FieldMappingDefinition(sourceField, f));
                        }
                    }
                    else
                    {
                        var sourceField = sourceTable.GetField(f.Name);
                        FieldMappingDefinitions.Add(new FieldMappingDefinition(sourceField, f));
                    }
                }
                catch(MigrationException ex)
                {
                    if(ex.ErrorCode == MigrationExceptionCodes.DATABASE_ERROR_FIELD_NOT_FOUND)
                    {
                        // TODO: write log
                        //Console.WriteLine(sourceTable.Name + " -> " + destinationTable.Name);
                    }
                }
            });
        }
    }
}
