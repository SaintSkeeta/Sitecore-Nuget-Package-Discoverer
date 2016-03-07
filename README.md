# Sitecore-Nuget-Package-Discoverer
A tool that attempts to resolve the 3rd party Nuget Packages that a Sitecore site uses. It does this by taking the DLL names and versions, and attempts to find them on the public nuget feed.

The tool can be run with the exe built, using:-

`NugetPackageResolver.exe "Path to Sitecore.Zip file" "Path to packages.config to be created"`

i.e
`NugetPackageResolver.exe "D:\_Sitecores\Sitecore 7.1 rev. 140130.zip" "D:\temp\packages.7.1.140130.config"`

For debugging, these arguments can be passed in on the Properties page of the NugetPackageResolver project, under the Debug tab.

A third parameter for a single DLL is optional. 
i.e
`NugetPackageResolver.exe "D:\_Sitecores\Sitecore 7.1 rev. 140130.zip" "D:\temp\packages.7.1.140130.config" "Newtonsoft.Json.dll`

This searches for a package for the DLL specified.