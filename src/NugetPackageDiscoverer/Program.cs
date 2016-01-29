using NugetPackageResolver.Models;
using NugetPackageResolver.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NugetPackageResolver.Implementations;
using System.IO;
using Ionic.Zip;
using System.Xml;

namespace NugetPackageResolver
{
    class Program
    {
        private static IVersionIdentifier _versionIdentifier { get; set; }
        private static IPackageDiscoverer _packageDiscoverer { get; set; }
        private static IVersionAuditor _dllAuditor { get; set; }
        private static IResultsFormatter _resultsFormatter { get; set; }

        public Program()
        {
            _versionIdentifier = new DLLVersionIdentifier();
            _packageDiscoverer = new NugetPackageDiscoverer();
            _dllAuditor = new DLLVersionAuditor();
            _resultsFormatter = new ResultsFormatter();
        }
        static int Main(string[] args)
        {
            _versionIdentifier = new DLLVersionIdentifier();
            _packageDiscoverer = new NugetPackageDiscoverer();
            _dllAuditor = new DLLVersionAuditor();
            _resultsFormatter = new ResultsFormatter();

            // Test if input arguments were supplied:
            if (args.Length != 2)
            {
                System.Console.WriteLine("Please enter a path to the bin directory as the 1st argument, and the path you want to generate the config for as the 2nd argument.");
                return 1;
            }

            string binDirectoryPath = ExtractBinDirectoryFromZip(args.Skip(0).First());

            return Process(binDirectoryPath, args.Skip(1).First());
        }

        #region ExtractZip
        private static string ExtractBinDirectoryFromZip(string zipPath)
        {
            var tmp = Path.GetTempFileName() + ".dir";
            try
            {
                Directory.CreateDirectory(tmp);

                using (var zip = new ZipFile(zipPath))
                {
                    foreach (var entry in zip.SelectEntries("*/Website/bin/*.dll"))
                    {
                        var fileName = Path.GetFileName(entry.FileName);
                        var outputFilePath = Path.Combine(tmp, fileName);
                        Save(entry, outputFilePath);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Could not extract bin directory from Zip.");
            }

            return tmp + "/";
        }

        private static void Save(ZipEntry entry, string outputFilePath)
        {
            using (var input = entry.OpenReader())
            {
                var folderPath = Path.GetDirectoryName(outputFilePath);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                using (Stream output = File.OpenWrite(outputFilePath))
                {
                    CopyStream(input, output);
                }
            }
        }

        private static void CopyStream(Stream input, Stream output)
        {
            var buffer = new byte[8 * 1024];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }
        }
        #endregion

        private static int Process(string binDirectoryPath, string configSaveFilename)
        {
            string filter = "*";

            List<NugetPackageInfo> discoveredNugetPackages = new List<NugetPackageInfo>();
            List<DLLIssueInfo> issues = new List<DLLIssueInfo>();

            // get versions of all DLLs
            // i.e get DLLs not starting with Sitecore*
            IEnumerable<VersionInfo> dllsInWebRoot = _versionIdentifier.GetDLLVersions(binDirectoryPath, filter);
            List<VersionInfo> remainingDLLsToCheck = dllsInWebRoot.ToList();

            // extract DLLs into known groups from packages, and resolve them out of the list
            // i.e Lucene.Contrib contains Lucene.Net.Contrib*
            List<PackageSearchResult> resolvedPackagesFromGroups = _packageDiscoverer.ResolveKnownPackageGroups(ref remainingDLLsToCheck);
            discoveredNugetPackages.AddRange(resolvedPackagesFromGroups.Where(x => x.NugetPackage != null).Select(x => x.NugetPackage));
            issues.AddRange(resolvedPackagesFromGroups.Where(x => x.Issue != null).Select(x => x.Issue));

            // convert known DLL to Nuget package conversions
            // i.e System.* to Microsoft.AspNet.*
            List<PackageSearchResult> resolvedPackagesFromNames = _packageDiscoverer.ResolveKnownPackageNames(ref remainingDLLsToCheck);
            discoveredNugetPackages.AddRange(resolvedPackagesFromNames.Where(x => x.NugetPackage != null).Select(x => x.NugetPackage));
            issues.AddRange(resolvedPackagesFromNames.Where(x => x.Issue != null).Select(x => x.Issue));

            //TODO: beta package versions DotNetOpenAuth. Also check DLL against package DLL.
            // TODO: System.Net.Http - says need newer version of Nuget

            // TODO: version mismatches? duplicates?
            // i.e WebApi 4.0.20710 or 4.0.30506 or 4.0.40810?

            // TODO: group all three above into one method, dependent elsewhere
            
            while (remainingDLLsToCheck.Any())
            {
                VersionInfo dllVersionInfo = remainingDLLsToCheck.First();

                // find corresponding package per DLL

                // search nuget.org for package with the same name as DLL
                // see if matching version is available on nuget.org
                PackageSearchResult searchResult = _packageDiscoverer.SearchForPackage(dllVersionInfo);
                
                NugetPackageInfo nugetPackage = searchResult.NugetPackage;
                if (nugetPackage != null)
                {
                    // check for other DLLs that exist in this package
                    List<DLLIssueInfo> extraDLLIssues = _dllAuditor.AuditDLLsAgainstWebroot(nugetPackage.DLLsInPackage, dllsInWebRoot);
                    issues.AddRange(extraDLLIssues);

                    foreach (var extraDLL in nugetPackage.DLLsInPackage)
                    {
                        remainingDLLsToCheck.Remove(extraDLL);
                    }

                    discoveredNugetPackages.Add(nugetPackage);                
                }
                else
                {
                    issues.Add(searchResult.Issue);
                }

                remainingDLLsToCheck.Remove(dllVersionInfo);  // so we can continue the loop
            }

            // TODO: check for dependency packages for extra validation
            //foreach(var nugetPackage in discoveredNugetPackages)
            //{
            //    // flatten out dependency packages (so we have a straight list)
            //    List<NugetPackageInfo> dependantPackages = _packageDiscoverer.GetFlattenedDependencyPackages(nugetPackage);

            //    foreach(var dependency in dependantPackages)
            //    {
            //        // extra auditing. make sure the package we've already discovered is compatible with the dependency package
            //        if (discoveredNugetPackages.Exists(package => package.Info.Name.Equals(dependency.Info.Name)))
            //        {

            //        }
            //        else
            //        {
            //            //TODO: issues.Add(error: dependency package doesn't exist in webroot);
            //        }
            //    }
            //}


            // run report
            XmlDocument packagesXml = _resultsFormatter.CreatePackagesDotConfigFile(discoveredNugetPackages.OrderBy(x => x.Info.Name));
            string comment = _resultsFormatter.CreateIssuesComment(issues.OrderBy(x => x.DLLInSitecore.Name));

            XmlComment commentXml = packagesXml.CreateComment(comment);
            packagesXml.DocumentElement.AppendChild(commentXml);

            packagesXml.Save(configSaveFilename);

            return 0;
        }
    }
}
