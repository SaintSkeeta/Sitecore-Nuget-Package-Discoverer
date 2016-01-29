using NugetPackageResolver.Interfaces;
using NugetPackageResolver.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NugetPackageResolver.Implementations
{
    public class DLLVersionIdentifier : IVersionIdentifier
    {
        public IEnumerable<VersionInfo> GetDLLVersions(string binDirectoryPath, string pattern)
        {
            System.Console.WriteLine(binDirectoryPath);
            // find all DLLs in bin folder that match the given filter
            //string pattern = "^(?!(Sitecore)).*(.dll)$";
            var matches = Directory.GetFiles(binDirectoryPath)
                            .Select(path => Path.GetFileName(path))
                            .Where(path => Regex.Match(path, "^(?!(Sitecore)).*(.dll)$", RegexOptions.IgnoreCase).Success);

            var dllVersionList = new List<VersionInfo>();
            // find the version numbers of these DLLs
            foreach(string file in matches)
            {
                string filename = Assembly.LoadFrom(binDirectoryPath + file).GetName().Name;

                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(binDirectoryPath + file);
                string version = fvi.FileVersion;
                Version ver = new Version(fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, fvi.FilePrivatePart);

                //Version ver = Assembly.LoadFrom(binDirectoryPath + file).GetName().Version;

                VersionInfo dllVersion = new VersionInfo() { Name = filename, VersionNumber = ver };
                dllVersionList.Add(dllVersion);
            }

            return dllVersionList;
        }
    }
}
