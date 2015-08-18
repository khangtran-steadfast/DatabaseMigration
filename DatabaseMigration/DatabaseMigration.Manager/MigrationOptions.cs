using DatabaseMigration.Infrastructure.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DatabaseMigration.Manager
{
    [XmlRoot("Configurations")]
    public class MigrationOptions
    {
        [XmlElement]
        public string ServerName { get; set; }

        [XmlElement]
        public string InstanceName { get; set; }

        [XmlArray("ExplicitTableMappings")]
        [XmlArrayItem("TableMapping", typeof(TableMappingConfiguration))]
        public List<TableMappingConfiguration> ExplicitTableMappings { get; set; }

        //[XmlArray("ExplicitSourceDatabaseReferences")]
        //[XmlArrayItem("Reference", typeof(ReferenceConfiguration))]
        //public List<ReferenceConfiguration> ExplicitSourceDatabaseReferences { get; set; }
    }
}
