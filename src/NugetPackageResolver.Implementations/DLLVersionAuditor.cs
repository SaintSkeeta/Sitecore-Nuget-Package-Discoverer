using NugetPackageResolver.Interfaces;
using NugetPackageResolver.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetPackageResolver.Implementations
{
    public class DLLVersionAuditor : IVersionAuditor
    {
        public List<DLLIssueInfo> AuditDLLsAgainstWebroot(IEnumerable<VersionInfo> dllVersionInfosFromPackage, IEnumerable<VersionInfo> dllsInWebroot) //, List<VersionInfo> remainingDLLsToBeChecked)
        {
            List<DLLIssueInfo> dllIssues = new List<DLLIssueInfo>();
            foreach (var versionInfo in dllVersionInfosFromPackage)
            {
                // does DLL also exist within Sitecore webroot
                VersionInfo dllInSitecore = dllsInWebroot.Where(x => x.Name.Equals(versionInfo.Name)).SingleOrDefault();
                if (dllInSitecore == null)
                {
                    dllIssues.Add(new DLLIssueInfo() { DLLFromPackage = versionInfo, Issue = DLLIssue.DLLDoesNotExistInWebroot });
                    continue;
                }

                // with correct version number? 
                bool doesVersionNumberMatch = dllInSitecore.VersionNumber.Equals(versionInfo.VersionNumber);
                if (!doesVersionNumberMatch)
                {
                    dllIssues.Add(new DLLIssueInfo() { DLLFromPackage = versionInfo, Issue = DLLIssue.DLLExistsButVersionIsWrong, DLLInSitecore = dllInSitecore });
                }
            }
            return dllIssues;
        }
    }
}
