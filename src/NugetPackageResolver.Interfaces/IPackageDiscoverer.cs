using NugetPackageResolver.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetPackageResolver.Interfaces
{
    public interface IPackageDiscoverer
    {
        PackageSearchResult SearchForPackage(VersionInfo dllVersionInfo);

        List<NugetPackageInfo> GetFlattenedDependencyPackages(NugetPackageInfo nugetPackage);

        List<PackageSearchResult> ResolveKnownPackageGroups(ref List<VersionInfo> remainingDLLsToCheck);

        List<PackageSearchResult> ResolveKnownPackageNames(ref List<VersionInfo> remainingDLLsToCheck);
    }
}
