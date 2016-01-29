using NugetPackageResolver.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetPackageResolver.Interfaces
{
    public interface IVersionIdentifier
    {
        IEnumerable<VersionInfo> GetDLLVersions(string binDirectoryPath, string filter);
    }
}
