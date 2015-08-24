using DatabaseMigration.Manager.Constants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StringInject;

namespace DatabaseMigration.Manager.ScriptGenerator
{
    class BatGenerator
    {
        public static string GenerateSqlExecuteScript(List<string> scriptNames, string serverName, string instanceName, string outputPath)
        {
            StringBuilder batBuilder = new StringBuilder();

            batBuilder.AppendLine(string.Format(BatTemplates.DELETE_FILE, @"Migration_Output.txt"));

            for (int i = 0; i < scriptNames.Count; i++)
            {
                string scriptName = scriptNames[i];

                string scriptPath = Path.Combine(Path.GetFullPath(outputPath), scriptName);
                batBuilder.AppendLine(string.Format(BatTemplates.ECHO_CONSOLE, scriptName));
                batBuilder.AppendLine(string.Format(BatTemplates.ECHO_FILE_APPEND, scriptName, @"Migration_Output.txt"));
                batBuilder.AppendLine(BatTemplates.EXECUTE_SQL.Inject(new
                {
                    ServerName = serverName,
                    InstanceName = instanceName,
                    ScriptPath = scriptName,
                    OutputPath = @"Migration_Output.txt"
                }));
            }

            string content = string.Format(BatTemplates.BAT, batBuilder.ToString());
            return content;
        }
    }
}
