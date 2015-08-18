using DatabaseMigration.Manager;
using DatabaseMigration.Infrastructure.Configurations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace DatabaseMigration.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            LicenseAspose();

            string sourceConnectionString = ConfigurationManager.ConnectionStrings["SourceDatabase"].ConnectionString;
            string destinationConnectionString = ConfigurationManager.ConnectionStrings["DestinationDatabase"].ConnectionString;

            MigrationOptions options = LoadConfigurations();
            MigrationManager manager = new MigrationManager(sourceConnectionString, destinationConnectionString, options);
            manager.GenerateMigrationScripts();
        }

        private static MigrationOptions LoadConfigurations()
        {
            MigrationOptions result = null;
            string path = "configurations.xml";

            using(StreamReader reader = new StreamReader(path))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(MigrationOptions));
                result = (MigrationOptions)serializer.Deserialize(reader);
            }

            return result;
        }

        private static void LicenseAspose()
        {
            Aspose.Words.License word = new Aspose.Words.License();
            word.SetLicense("Aspose.Total.lic");

            Aspose.Cells.License excel = new Aspose.Cells.License();
            excel.SetLicense("Aspose.Total.lic");
        }
    }
}
