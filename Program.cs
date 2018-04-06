using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using System.IO.Compression;
using System.Collections.Generic;

namespace NajdiNET.sxc.Rename
{
    class Program
    {
        static void Main(string[] args)
        {
            // Read parameters
            var fnZip = "App.zip";
            var newAppName = "NewApp";
            if (args.Length > 0) { fnZip = args[0]; }
            if (args.Length > 1) { newAppName = args[1]; }

            // Update ZIP file
            using (var archive = new ZipArchive(File.Open(fnZip, FileMode.Open, FileAccess.ReadWrite), ZipArchiveMode.Update))
            {
                if (archive.Entries.Count > 0)
                {
                    // Get Old App Name and display values
                    var appName = archive.Entries[0].FullName.Split(Path.DirectorySeparatorChar).ElementAt(1);
                    Console.WriteLine("Old app name : " + appName);
                    Console.WriteLine("New app name : " + newAppName);

                    // Move All Files To new App Name Folder
                    var files = archive.Entries.ToArray();
                    foreach (var f in files)
                    {
                        var newEntry = archive.CreateEntry(f.FullName.Replace(np(appName),np(newAppName)));
                        using (var a = f.Open())
                        {
                            using (var b = newEntry.Open())
                            {
                                a.CopyTo(b);
                            }
                        }
                        f.Delete();
                    }

                    // Read App.xml file to string
                    var xmlFileEntry = archive.GetEntry(string.Format(@"Apps\{0}\App.xml",newAppName));
                    string xmlText;
                    using (var stream = xmlFileEntry.Open())
                    {
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            xmlText = reader.ReadToEnd();
                        }
                    }
                    xmlFileEntry.Delete();

                    // Write updated App.xml back to ZIP
                    var newXmlFileEntry = archive.CreateEntry(xmlFileEntry.FullName);
                    using (StreamWriter writer = new StreamWriter(newXmlFileEntry.Open()))
                    {
                        writer.Write(ChangeXMLGuids(xmlText, appName, newAppName));
                    }
                }
            }
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.ReadLine();
            }
        }

        // Helper functions
        private static string ChangeXMLGuids(string xmlText, string AppName, string newAppName)
        {
            Guid tGuid;
            HashSet<string> gLIst = new HashSet<string>();
            XDocument xmlDoc = XDocument.Load(new StringReader(xmlText));

            var attrNames = new[] { "Guid", "EntityGUID", "KeyGuid", "AttributeSetStaticName", "AttributeSetName" };

            foreach (var attr in attrNames)
            {
                var elements = xmlDoc.Descendants().Where(x => x.Attribute(attr) != null);
                foreach (var element in elements)
                {
                    var g = element.Attribute(attr).Value;
                    if (Guid.TryParse(g, out tGuid))
                    {
                        //Skip For Some Exceptions
                        var tAttr = element.Attribute("AttributeSetName");
                        if (attr == "AttributeSetStaticName" && tAttr != null && tAttr.Value == "|Config ToSic.Eav.DataSources.SqlDataSource") continue;
                        //Add Valid Guid to HashSet
                        gLIst.Add(g);
                    }
                }
            }

            // Change App Name
            xmlText = xmlText.Replace("Value=\""+AppName+"\"", "Value=\"" + newAppName + "\"");

            // Change required Guids
            foreach (var g in gLIst)
                xmlText = xmlText.Replace(g, Guid.NewGuid().ToString());

            return xmlText;
        }

        public static string np(string name)
        {
            return @"\" + name + @"\";
        }


    }
}

