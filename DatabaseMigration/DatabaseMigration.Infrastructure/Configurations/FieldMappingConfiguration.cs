﻿using DatabaseMigration.Infrastructure.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DatabaseMigration.Infrastructure.Configurations
{
    public class FieldMappingConfiguration
    {
        [XmlAttribute("SourceField")]
        public string SourceFieldName { get; set; }

        [XmlAttribute("DestinationField")]
        public string DestinationFieldName { get; set; }

        [XmlAttribute]
        public FieldMappingType Type { get; set; }
    }
}
