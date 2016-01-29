using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetPackageResolver.Models
{
    public class VersionInfo
    {
        public string Name { get; set; }

        public Version VersionNumber { get; set; }
    }
}
