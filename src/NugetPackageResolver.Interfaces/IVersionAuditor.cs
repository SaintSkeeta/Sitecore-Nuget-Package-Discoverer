using NugetPackageResolver.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetPackageResolver.Interfaces
{
    public interface IVersionAuditor
    {
        List<DLLIssueInfo> AuditDLLsAgainstWebroot(IEnumerable<VersionInfo> dllVersionInfosFromPackage, IEnumerable<VersionInfo> dllsInWebroot);
    }
}
