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

            scriptNames.ForEach(s =>
            {
                string scriptPath = Path.Combine(Path.GetFullPath(outputPath), s);
                batBuilder.AppendLine(string.Format(BatTemplates.ECHO_CONSOLE, s));
                batBuilder.AppendLine(string.Format(BatTemplates.ECHO_FILE, s, @"C:\Output.txt"));
                batBuilder.AppendLine(BatTemplates.EXECUTE_SQL.Inject(new
                {
                    ServerName = serverName,
                    InstanceName = instanceName,
                    ScriptPath = scriptPath,
                    OutputPath = @"C:\Output.txt"
                }));
            });

            string content = string.Format(BatTemplates.BAT, batBuilder.ToString());
            return content;
        }
    }
}
