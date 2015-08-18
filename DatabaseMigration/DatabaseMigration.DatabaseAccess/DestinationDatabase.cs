using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseMigration.DatabaseAccess
{
    public class DestinationDatabase : Database
    {
        public DestinationDatabase(string connectionString) : base(connectionString)
        {

        }
    }
}
