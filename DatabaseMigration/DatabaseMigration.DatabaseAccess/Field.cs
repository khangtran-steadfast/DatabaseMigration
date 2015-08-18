using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseMigration.DatabaseAccess
{
    public class Field
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public FieldType Type { get; set; }
        public Reference Reference { get; set; }

        public Field(string name, string dataType)
        {
            Name = name;
            DataType = dataType;
        }
    }

    [Flags]
    public enum FieldType
    {
        Normal = 1,
        Identity = 2,
        PrimaryKey = 4,
        ForeignKey = 8,
        Unique = 16
        //Blob = 16
    }
}
