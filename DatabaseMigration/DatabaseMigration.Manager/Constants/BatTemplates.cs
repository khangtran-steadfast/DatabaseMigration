using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseMigration.Manager.Constants
{
    public class BatTemplates
    {
        public const string DELETE_FILE = @"if exist ""{0}"" del ""{0}""";

        public const string ECHO_CONSOLE = @"echo {0}";

        public const string ECHO_FILE_APPEND = @"echo {0} >> {1}";

        public const string EXECUTE_SQL = @"sqlcmd -S {ServerName}\{InstanceName} -i {ScriptPath} >> {OutputPath}";

        public const string BAT =
@"echo off

{0}

set /p delExit=Press the ENTER key to exit...:
";
    }
}
