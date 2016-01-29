using NugetPackageResolver.Interfaces;
using NugetPackageResolver.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace NugetPackageResolver.Implementations
{
    public class ResultsFormatter : IResultsFormatter
    {
        public System.Xml.XmlDocument CreatePackagesDotConfigFile(IEnumerable<Models.NugetPackageInfo> packages)
        {
            XmlDocument doc = new XmlDocument();

            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            XmlElement packagesElement = doc.CreateElement(string.Empty, "packages", string.Empty);
            doc.AppendChild(packagesElement);

            foreach (NugetPackageInfo package in packages)
            {
                XmlElement packageElement = doc.CreateElement(string.Empty, "package", string.Empty);
                packagesElement.AppendChild(packageElement);

                XmlAttribute idAttribute = doc.CreateAttribute("id");
                idAttribute.Value = package.Info.Name;
                packageElement.Attributes.Append(idAttribute);

                XmlAttribute versionAttribute = doc.CreateAttribute("version");
                versionAttribute.Value = package.Info.VersionNumber.ToString();
                packageElement.Attributes.Append(versionAttribute);

                XmlAttribute frameworkAttribute = doc.CreateAttribute("targetFramework");
                frameworkAttribute.Value = "net45";
                packageElement.Attributes.Append(frameworkAttribute);
            }

            return doc;
        }

        public string CreateIssuesComment(IEnumerable<Models.DLLIssueInfo> issues)
        {
            string comment = " The following issues were found when attempting to discover matching Nuget packages for DLLs.\n";

            foreach (DLLIssueInfo issue in issues)
            {
                comment += "      " + issue.DLLInSitecore.Name + ", v" + issue.DLLInSitecore.VersionNumber.ToString() + "\n           ";

                switch(issue.Issue)
                {
                    case DLLIssue.NoCorrespondingPackageFound:
                        comment += "No Package matching the DLL Name was found.";
                        break;

                    case DLLIssue.NoCorrespondingVersionForPackageFound:
                        comment += "Matching Package name was found, however there was no matching version found.";
                        break;

                    default:
                        comment += "An unknown error occurred";
                        break;

                }

                comment += "\n";
            }


            return comment;
        }
    }
}

