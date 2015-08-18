using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseMigration.Infrastructure.Enums
{
    public enum FieldMappingType
    {
        Simple,
        BlobToBlobPointer,
        BlobToString
    }
}
