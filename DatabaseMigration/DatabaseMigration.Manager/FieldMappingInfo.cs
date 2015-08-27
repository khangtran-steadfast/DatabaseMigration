using DatabaseMigration.DatabaseAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseMigration.Manager
{
    class FieldMappingInfo
    {
        public SourceTable SourceTable { get; set; }
        public DestinationTable DestinationTable { get; set; }
        public Field SourceField { get; set; }
        public Field DestinationField { get; set; }
    }
}
