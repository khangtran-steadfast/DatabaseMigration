using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DatabaseMigration.Infrastructure.Configurations
{
    public class ReferenceConfiguration
    {
        [XmlAttribute("OriginTable")]
        public string OriginTableName { get; set; }

        [XmlAttribute("OriginField")]
        public string OriginFieldName { get; set; }

        [XmlAttribute("ReferenceTable")]
        public string ReferenceTableName { get; set; }

        [XmlAttribute("ReferenceField")]
        public string ReferenceFieldName { get; set; }
    }
}
