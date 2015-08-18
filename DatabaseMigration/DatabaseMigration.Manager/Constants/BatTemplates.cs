using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseMigration.Manager.Constants
{
    public class BatTemplates
    {
        public const string ECHO = @"echo {0}";

        public const string EXECUTE_SQL = @"sqlcmd -S {ServerName}\{InstanceName} -i {ScriptPath}";

        public const string BAT =
@"echo off

{0}

set /p delExit=Press the ENTER key to exit...:
";
    }
}
