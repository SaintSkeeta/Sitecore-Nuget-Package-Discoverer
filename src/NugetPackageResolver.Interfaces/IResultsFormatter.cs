using NugetPackageResolver.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NugetPackageResolver.Interfaces
{
    public interface IResultsFormatter
    {
        XmlDocument CreatePackagesDotConfigFile(IEnumerable<NugetPackageInfo> packages);

        string CreateIssuesComment(IEnumerable<DLLIssueInfo> issues);
    }
}
