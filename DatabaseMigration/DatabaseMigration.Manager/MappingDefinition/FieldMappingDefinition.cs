using DatabaseMigration.DatabaseAccess;
using DatabaseMigration.Infrastructure.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseMigration.Manager.MappingDefinition
{
    public class FieldMappingDefinition
    {
        private Field _sourceField;
        public Field SourceField
        {
            get { return _sourceField; }
        }

        private Field _destinationField;
        public Field DestinationField
        {
            get { return _destinationField; }
        }

        private FieldMappingType _type;
        public FieldMappingType Type
        {
            get { return _type; }
        }

        public FieldMappingDefinition(Field sourceField, Field destinationField, FieldMappingType type = FieldMappingType.Simple)
        {
            _sourceField = sourceField;
            _destinationField = destinationField;
            _type = type;
        }
    }
}
