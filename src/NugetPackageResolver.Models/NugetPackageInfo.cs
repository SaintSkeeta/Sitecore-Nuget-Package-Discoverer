using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetPackageResolver.Models
{
    public class NugetPackageInfo
    {
        public VersionInfo Info { get; set; }

        public List<VersionInfo> PackageDependencies { get; set; }

        public List<VersionInfo> DLLsInPackage { get; set; }

        public NugetPackageInfo()
        {
            DLLsInPackage = new List<VersionInfo>();
            PackageDependencies = new List<VersionInfo>();
        }
    }
}
