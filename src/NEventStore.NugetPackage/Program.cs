using System;

namespace NEventStore.NugetPackage
{
    using System.IO;
    using System.Reflection;

    public class Program
    {
        public static void Main(string[] args)
        {
            var nugetTemplateFile = args[0];
            var nugetFile = args[1];

            var version = GetVersion();

            using (var sr = new StreamReader(nugetTemplateFile))
            {
                try
                {
                    var templateFile = sr.ReadToEnd();
                    templateFile = templateFile.Replace("@NEventStoreVersion@", version);

                    using (var sw = new StreamWriter(nugetFile))
                    {
                        try
                        {
                            sw.Write(templateFile);
                        }
                        finally
                        {
                            sw.Close();
                        }
                    }
                }
                finally
                {
                    sr.Close();
                }
            }
        }

        private static string GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyInformationVersion = Attribute.GetCustomAttributes(assembly, typeof(AssemblyInformationalVersionAttribute));

            if (assemblyInformationVersion.Length > 0)
            {
                var informationVersion = (AssemblyInformationalVersionAttribute)assemblyInformationVersion[0];

                return informationVersion.InformationalVersion;
            }

            return assembly.GetName().Version.ToString(3);
        }
    }
}
