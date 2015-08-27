using DatabaseMigration.Infrastructure.Configurations;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseMigration.DatabaseAccess
{
    public class SourceTable : Table
    {
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
    }
}
