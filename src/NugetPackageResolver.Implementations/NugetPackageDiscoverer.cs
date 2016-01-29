using NuGet;
using NugetPackageResolver.Interfaces;
using NugetPackageResolver.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NugetPackageResolver.Implementations
{
    public class NugetPackageDiscoverer : IPackageDiscoverer
    {
        /// <summary>
        /// Searches for the package on nuget.org (the public Nuget repository)
        /// </summary>
        /// <param name="dllVersionInfo"></param>
        public PackageSearchResult SearchForPackage(VersionInfo dllVersionInfo)
        {
            string packageID = dllVersionInfo.Name;

            // http://blog.nuget.org/20130520/Play-with-packages.html
            //Connect to the official package repository
            IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2");

            //Get the list of all NuGet packages with the ID provided
            List<IPackage> packages = repo.FindPackagesById(packageID).ToList();
            if (!packages.Any())
            {
                // error package not found
                Console.WriteLine("- Package Not Found: " + dllVersionInfo.Name);
                return new PackageSearchResult()
                            {
                                Issue = new DLLIssueInfo() { Issue = DLLIssue.NoCorrespondingPackageFound, DLLInSitecore = dllVersionInfo }
                            };
            }

            IPackage matchingPackage = this.FindMatchingPackageVersion(packages, dllVersionInfo);
            if (matchingPackage == null) // || IsNotCompleteVersion) - DotNetOpenAuth issue
            {
                Console.WriteLine("--- Version Not Found: " + dllVersionInfo.Name + ", " + dllVersionInfo.VersionNumber.ToString());

                return new PackageSearchResult()
                {
                    Issue = new DLLIssueInfo() { Issue = DLLIssue.NoCorrespondingVersionForPackageFound, DLLInSitecore = dllVersionInfo }
                };
            }

            
            var nugetPackageInfo = CreateNugetPackageInfoFromIPackage(matchingPackage);
            Console.WriteLine("PACKAGE FOUND: " + nugetPackageInfo.Info.Name + ", " + nugetPackageInfo.Info.VersionNumber.ToString());
            // sholmesby: HACK for single DLL test
            nugetPackageInfo.DLLsInPackage.Add(dllVersionInfo);
            return new PackageSearchResult()
            {
                NugetPackage = nugetPackageInfo
            }; 
        }

        private IPackage FindMatchingPackageVersion(IEnumerable<IPackage> packages, VersionInfo dllVersionInfo)
        {
            IPackage matchingPackage = packages.Where(package => package.Version.Version.Equals(dllVersionInfo.VersionNumber)).FirstOrDefault();
            if (matchingPackage == null)
            {
                // Major, Minor, Build

                Version buildVersionMatch = new Version(dllVersionInfo.VersionNumber.Major,
                                                 dllVersionInfo.VersionNumber.Minor,
                                                 dllVersionInfo.VersionNumber.Build, 0);

                matchingPackage = packages.Where(package => package.Version.Version.Equals(buildVersionMatch)).FirstOrDefault();

                if (matchingPackage == null)
                {
                    // Major, Minor, BuildDigit1 (i.e 5.2.30706 becomes 5.2.3)
                    int firstDigitBuildNumber = Math.Abs(dllVersionInfo.VersionNumber.Build);
                    while (firstDigitBuildNumber >= 10)
                        firstDigitBuildNumber /= 10;

                    Version firstDigitiBuildVersionMatch = new Version(dllVersionInfo.VersionNumber.Major,
                                                     dllVersionInfo.VersionNumber.Minor,
                                                     firstDigitBuildNumber, 0);

                    matchingPackage = packages.Where(package => package.Version.Version.Equals(firstDigitiBuildVersionMatch)).FirstOrDefault();

                    if (matchingPackage == null)
                    {

                        // Major, Minor
                        Version minorVersionMatch = new Version(dllVersionInfo.VersionNumber.Major,
                                                         dllVersionInfo.VersionNumber.Minor, 0, 0);

                        matchingPackage = packages.Where(package => package.Version.Version.Equals(minorVersionMatch)).FirstOrDefault();

                        if (matchingPackage == null)
                        {
                            // Major
                            Version majorVersionMatch = new Version(dllVersionInfo.VersionNumber.Major, 0, 0, 0);

                            matchingPackage = packages.Where(package => package.Version.Version.Equals(majorVersionMatch)).FirstOrDefault();
                        }
                    }
                }
            }

            return matchingPackage;
        }
        
        private NugetPackageInfo CreateNugetPackageInfoFromIPackage(IPackage matchingPackage)
        {
            var packageInfo = new NugetPackageInfo()
            {
                Info = new VersionInfo() { Name = matchingPackage.Id, VersionNumber = matchingPackage.Version.Version }
            };

            // sholmesby: May need to download packages to filesystem here. Then we can get the DLL version number.

            // sholmesby: how can we find out the version number of this assembly?
            //foreach(var assembly in matchingPackage.AssemblyReferences)
            //{
            //    Assembly.LoadFrom(  assembly.GetStream()
            //}

            int uniqueDLLCount = matchingPackage.AssemblyReferences.Select(x => x.EffectivePath).Distinct().Count();
            if (uniqueDLLCount > 1)
            {
                Console.WriteLine("---- '" + matchingPackage.Id + "' may contain more than 1 DLL.");
            }

            // sholmesby: find which package would be installed? could be any number of packages throughout a range of the VersionSpec.
            //matchingPackage.DependencySets.Select(package => package.Dependencies.Where(pack => pack.VersionSpec)

            return packageInfo;
        }

        public List<NugetPackageInfo> GetFlattenedDependencyPackages(NugetPackageInfo nugetPackage)
        {
            throw new NotImplementedException();
            //return new List<NugetPackageInfo>();
        }

        public List<PackageSearchResult> ResolveKnownPackageGroups(ref List<VersionInfo> remainingDLLsToCheck)
        {
            // Lucene.Net.Contrib contains
            // - Lucene.Net.Contrib.Analyzers
            // - Lucene.Net.Contrib.Core
            // - Lucene.Net.Contrib.FastVectorHighlighter
            // - Lucene.Net.Contrib.Highlighter
            // - Lucene.Net.Contrib.Memory
            // - Lucene.Net.Contrib.Queries
            // - Lucene.Net.Contrib.Regex
            // - Lucene.Net.Contrib.SimpleFacetedSearch
            // - Lucene.Net.Contrib.Snowball
            // - Lucene.Net.Contrib.SpellChecker
            List<PackageSearchResult> searchResults = new List<PackageSearchResult>();
            VersionInfo luceneContribDLL = remainingDLLsToCheck.FirstOrDefault(dll => dll.Name.StartsWith("Lucene.Net.Contrib"));
            if (luceneContribDLL != null)
            {
                var luceneContribSearchResult = this.SearchForPackage(new VersionInfo() { Name = "Lucene.Net.Contrib", VersionNumber = luceneContribDLL.VersionNumber });
                searchResults.Add(luceneContribSearchResult);
                
                var luceneContribDLLs = remainingDLLsToCheck.Where(dll => dll.Name.StartsWith("Lucene.Net.Contrib")).ToList();
                foreach(var dll in luceneContribDLLs)
                {
                    remainingDLLsToCheck.Remove(dll);
                }
            }

            // Google.Apis contans
            // - Google.Apis
            // - Google.Apis.Authentication.OAuth
            var googleDLLs = remainingDLLsToCheck.Where(dll => dll.Name.Equals("Google.Apis") || dll.Name.Equals("Google.Apis.Authentication.OAuth2")).ToList();
            if (googleDLLs.Any())
            {
                var searchResult = this.SearchForPackage(new VersionInfo() { Name = "Google.Apis", VersionNumber = googleDLLs.First().VersionNumber });
                searchResults.Add(searchResult);

                foreach (var dll in googleDLLs)
                {
                    remainingDLLsToCheck.Remove(dll);
                }
            }

            // Microsoft.AspNet.WebPages contains
            // - System.Web.Helpers
            // - System.Web.WebPages
            // - System.Web.WebPages.Deployment
            // - System.Web.WebPages.Razor
            var webPagesDLLs = remainingDLLsToCheck.Where(dll => dll.Name.StartsWith("System.Web.WebPages")).ToList();
            if (webPagesDLLs.Any())
            {
                var webPagesSearchResult = this.SearchForPackage(new VersionInfo() { Name = "Microsoft.AspNet.WebPages", VersionNumber = webPagesDLLs.First().VersionNumber });
                searchResults.Add(webPagesSearchResult);

                foreach (var dll in webPagesDLLs)
                {
                    remainingDLLsToCheck.Remove(dll);
                }

                var webHelpers = remainingDLLsToCheck.SingleOrDefault(dll => dll.Name.Equals("System.Web.Helpers"));
                if (webHelpers != null)
                {
                    remainingDLLsToCheck.Remove(webHelpers);
                }
            }

            // mongocharpdriver contains
            // - MongoDB.Bson
            // - MongoDB.Driver
            var mongoDLLs = remainingDLLsToCheck.Where(dll => dll.Name.StartsWith("MongoDB.")).ToList();
            if (mongoDLLs.Any())
            {
                var mongoDBSearchResult = this.SearchForPackage(new VersionInfo() { Name = "mongocsharpdriver", VersionNumber = mongoDLLs.First().VersionNumber });
                searchResults.Add(mongoDBSearchResult);

                // We know MongoDB 1.10 of the legacy driver is used up to Sitecore 8.1 Update 1.
                // If Sitecore update the driver to not use the legacy driver, we don't want to resolve with this package. (this may need to be changed).
                if (mongoDBSearchResult.NugetPackage != null)
                {
                    foreach (var dll in mongoDLLs)
                    {
                        remainingDLLsToCheck.Remove(dll);
                    }
                }
            }

            return searchResults;
        }

        public List<PackageSearchResult> ResolveKnownPackageNames(ref List<VersionInfo> remainingDLLsToCheck)
        {
            List<PackageSearchResult> searchResults = new List<PackageSearchResult>();

            // TODO: Extract these out into a single method.
            var antlrDLLs = remainingDLLsToCheck.Where(dll => dll.Name.StartsWith("Antlr3.Runtime")).ToList();
            if (antlrDLLs.Any())
            {
                var searchResult = this.SearchForPackage(new VersionInfo() { Name = "Antlr", VersionNumber = antlrDLLs.First().VersionNumber });
                searchResults.Add(searchResult);

                foreach (var dll in antlrDLLs)
                {
                    remainingDLLsToCheck.Remove(dll);
                }
            }

            // From Nuget page: The owner has unlisted this package. This could mean that the package is deprecated or shouldn't be used anymore.
            var componentArtDLLs = remainingDLLsToCheck.Where(dll => dll.Name.StartsWith("ComponentArt.Web.UI")).ToList();
            if (componentArtDLLs.Any())
            {
                var searchResult = this.SearchForPackage(new VersionInfo() { Name = "izenda.ComponentArt.Web.UI", VersionNumber = componentArtDLLs.First().VersionNumber });
                searchResults.Add(searchResult);

                foreach (var dll in componentArtDLLs)
                {
                    remainingDLLsToCheck.Remove(dll);
                }
            }

            // The version used in Sitecore 8.1 Update 1 is 2.1.0.1. This has direct references to EcmaScript.Net and Iesi.Collections.
            // Luckily the versions of these in Sitecore match what the dependencies are on the nuget package.
            // This seems to be fixed from the 2.2.0.1 version of YUICompressor.NET, and the DLLs are dependency packages.
            var yahooYuiDLLs = remainingDLLsToCheck.Where(dll => dll.Name.StartsWith("Yahoo.Yui.Compressor")).ToList();
            if (yahooYuiDLLs.Any())
            {
                var searchResult = this.SearchForPackage(new VersionInfo() { Name = "YUICompressor.NET", VersionNumber = yahooYuiDLLs.First().VersionNumber });
                searchResults.Add(searchResult);

                foreach (var dll in yahooYuiDLLs)
                {
                    remainingDLLsToCheck.Remove(dll);
                }
            }

            // maybe extract out the Microsoft DLLs into their own method
            var webHostDLLs = remainingDLLsToCheck.Where(dll => dll.Name.StartsWith("System.Web.Http.WebHost")).ToList();
            if (webHostDLLs.Any())
            {
                var searchResult = this.SearchForPackage(new VersionInfo() { Name = "Microsoft.AspNet.WebApi.WebHost", VersionNumber = webHostDLLs.First().VersionNumber });
                searchResults.Add(searchResult);

                foreach (var dll in webHostDLLs)
                {
                    remainingDLLsToCheck.Remove(dll);
                }
            }

            var formattingDLLs = remainingDLLsToCheck.Where(dll => dll.Name.StartsWith("System.Net.Http.Formatting")).ToList();
            if (formattingDLLs.Any())
            {
                var searchResult = this.SearchForPackage(new VersionInfo() { Name = "Microsoft.AspNet.WebApi.Client", VersionNumber = formattingDLLs.First().VersionNumber });
                searchResults.Add(searchResult);

                foreach (var dll in formattingDLLs)
                {
                    remainingDLLsToCheck.Remove(dll);
                }
            }

            var httpDLLs = remainingDLLsToCheck.Where(dll => dll.Name.Equals("System.Web.Http")).ToList();
            if (httpDLLs.Any())
            {
                var searchResult = this.SearchForPackage(new VersionInfo() { Name = "Microsoft.AspNet.WebApi.Core", VersionNumber = httpDLLs.First().VersionNumber });
                searchResults.Add(searchResult);

                foreach (var dll in httpDLLs)
                {
                    remainingDLLsToCheck.Remove(dll);
                }
            }

            var httpCorsDLLs = remainingDLLsToCheck.Where(dll => dll.Name.Equals("System.Web.Http.Cors")).ToList();
            if (httpCorsDLLs.Any())
            {
                var searchResult = this.SearchForPackage(new VersionInfo() { Name = "Microsoft.AspNet.WebApi.Cors", VersionNumber = httpCorsDLLs.First().VersionNumber });
                searchResults.Add(searchResult);

                foreach (var dll in httpCorsDLLs)
                {
                    remainingDLLsToCheck.Remove(dll);
                }
            }

            var optimizationDLLs = remainingDLLsToCheck.Where(dll => dll.Name.Equals("System.Web.Optimization")).ToList();
            if (optimizationDLLs.Any())
            {
                var searchResult = this.SearchForPackage(new VersionInfo() { Name = "Microsoft.AspNet.Web.Optimization", VersionNumber = optimizationDLLs.First().VersionNumber });
                searchResults.Add(searchResult);

                foreach (var dll in optimizationDLLs)
                {
                    remainingDLLsToCheck.Remove(dll);
                }
            }

            // group of DLLs, all of which might be needed
            var systemWebDLLs = remainingDLLsToCheck.Where(dll => dll.Name.StartsWith("System.Web")).ToList();
            if (systemWebDLLs.Any())
            {
                foreach (var dll in systemWebDLLs)
                {
                    var searchResult = this.SearchForPackage(new VersionInfo() { Name = dll.Name.Replace("System.Web.", "Microsoft.AspNet."), VersionNumber = dll.VersionNumber });
                    searchResults.Add(searchResult);

                    remainingDLLsToCheck.Remove(dll);
                }
            }

            return searchResults;
        }

    }
}
