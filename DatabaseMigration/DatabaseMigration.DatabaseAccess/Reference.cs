using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseMigration.DatabaseAccess
{
    public class Reference
    {
        private string _referenceName;
        public string ReferenceName
        {
            get { return _referenceName; }
        }

        private string _originTableName;
        public string OriginTableName
        {
            get { return _originTableName; }
        }

        private string _originFieldName;
        public string OriginFieldName
        {
            get { return _originFieldName; }
        }

        private string _referenceTableName;
        public string ReferenceTableName 
        {
            get { return _referenceTableName; } 
        }

        private string _referenceFieldName;
        public string ReferenceFieldName 
        {
            get { return _referenceFieldName; } 
        }

        public Reference(string referenceName, string originTableName, string originFieldName, string referenceTableName, string referenceFieldName)
        {
            _referenceName = referenceName;
            _originTableName = originTableName;
            _originFieldName = originFieldName;
            _referenceTableName = referenceTableName;
            _referenceFieldName = referenceFieldName;
        }
    }
}
