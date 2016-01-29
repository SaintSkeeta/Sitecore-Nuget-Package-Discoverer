using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NugetPackageResolver.Models
{
    public class PackageSearchResult
    {
        public DLLIssueInfo Issue { get; set; }

        public NugetPackageInfo NugetPackage { get; set; }
    }
}
