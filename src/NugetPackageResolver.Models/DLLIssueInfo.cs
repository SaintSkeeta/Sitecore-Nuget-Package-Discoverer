using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetPackageResolver.Models
{
    public class DLLIssueInfo
    {
        public VersionInfo DLLFromPackage { get; set; }
        
        public DLLIssue Issue { get; set; }

        public VersionInfo DLLInSitecore { get; set; }  //only filled out if DLLExistsButVersionIsWrong, or if NoCorrespondingPackageFound
    }
}
