using CycloneDX.Models;
using LCT.APICommunications;
using LCT.APICommunications.Model;
using LCT.ArtifactoryUploader.Model;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Directory = System.IO.Directory;

namespace LCT.ArtifactoryUploader
{
    public static class PackageUploadInformation
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static DisplayPackagesInfo GetComponentsToBePackages()
        {
            DisplayPackagesInfo displayPackagesInfo = new DisplayPackagesInfo();
            displayPackagesInfo.UnknownPackagesNpm = new List<ComponentsToArtifactory>();
            displayPackagesInfo.UnknownPackagesNuget = new List<ComponentsToArtifactory>();
            displayPackagesInfo.UnknownPackagesMaven = new List<ComponentsToArtifactory>();
            displayPackagesInfo.UnknownPackagesConan = new List<ComponentsToArtifactory>();
            displayPackagesInfo.UnknownPackagesPython = new List<ComponentsToArtifactory>();
            displayPackagesInfo.UnknownPackagesDebian = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesNpm = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesNuget = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesPython = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesMaven = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesConan = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesDebian = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesNpm = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesNuget = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesPython = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesMaven = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesConan = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesDebian = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesNpm = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesNuget = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesPython = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesMaven = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesConan = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesDebian = new List<ComponentsToArtifactory>();


            return displayPackagesInfo;

        }
        public static void DisplayPackageUploadInformation(DisplayPackagesInfo displayPackagesInfo)
        {
            string localPathforartifactory = GettPathForArtifactoryUpload();

            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesNpm, displayPackagesInfo.JfrogNotFoundPackagesNpm, displayPackagesInfo.SuccessfullPackagesNpm, displayPackagesInfo.JfrogFoundPackagesNpm, "Npm", localPathforartifactory);
            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesNuget, displayPackagesInfo.JfrogNotFoundPackagesNuget, displayPackagesInfo.SuccessfullPackagesNuget, displayPackagesInfo.JfrogFoundPackagesNuget, "Nuget", localPathforartifactory);
            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesMaven, displayPackagesInfo.JfrogNotFoundPackagesMaven, displayPackagesInfo.SuccessfullPackagesMaven, displayPackagesInfo.JfrogFoundPackagesMaven, "Maven", localPathforartifactory);
            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesConan, displayPackagesInfo.JfrogNotFoundPackagesConan, displayPackagesInfo.SuccessfullPackagesConan, displayPackagesInfo.JfrogFoundPackagesConan, "Conan", localPathforartifactory);
            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesPython, displayPackagesInfo.JfrogNotFoundPackagesPython, displayPackagesInfo.SuccessfullPackagesPython, displayPackagesInfo.JfrogFoundPackagesPython, "Poetry", localPathforartifactory);
            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesDebian, displayPackagesInfo.JfrogNotFoundPackagesDebian, displayPackagesInfo.SuccessfullPackagesDebian, displayPackagesInfo.JfrogFoundPackagesDebian, "Debian", localPathforartifactory);

        }
        public static string GettPathForArtifactoryUpload()
        {
            string localPathforartifactory = string.Empty;
            try
            {
                String Todaysdate = DateTime.Now.ToString("dd-MM-yyyy_ss");
                localPathforartifactory = $"{Directory.GetParent(Directory.GetCurrentDirectory())}\\ClearingTool\\ArtifactoryFiles\\{Todaysdate}\\";
                if (!Directory.Exists(localPathforartifactory))
                {
                    localPathforartifactory = Directory.CreateDirectory(localPathforartifactory).ToString();
                }
            }
            catch (IOException ex)
            {
                Logger.Error($"GettPathForArtifactoryUpload() ", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error($"GettPathForArtifactoryUpload() ", ex);
            }

            return localPathforartifactory;
        }
        private static void DisplaySortedForeachComponents(List<ComponentsToArtifactory> unknownPackages, List<ComponentsToArtifactory> JfrogNotFoundPackages, List<ComponentsToArtifactory> SucessfullPackages, List<ComponentsToArtifactory> JfrogFoundPackages, string name, string filename)
        {
            if (unknownPackages.Any() || JfrogNotFoundPackages.Any() || SucessfullPackages.Any() || JfrogFoundPackages.Any())
            {
                Logger.Info("\n" + name + ":\n");
                DisplayErrorForUnknownPackages(unknownPackages, name, filename);
                DisplayErrorForJfrogFoundPackages(JfrogFoundPackages);
                DisplayErrorForJfrogPackages(JfrogNotFoundPackages);
                DisplayErrorForSucessfullPackages(SucessfullPackages);
            }

        }
        private static void DisplayErrorForUnknownPackages(List<ComponentsToArtifactory> unknownPackages, string name, string filepath)
        {
            ProjectResponse projectResponse = new ProjectResponse();
            IFileOperations fileOperations = new FileOperations();
            var filename = Path.Combine(filepath, $"Artifactory_{FileConstant.artifactoryReportNotApproved}");

            var packageHandlers = new Dictionary<string, Action<List<ComponentsToArtifactory>, ProjectResponse, IFileOperations, string, string>>
    {
        { "Npm", GetNotApprovedNpmPackages },
        { "Nuget", GetNotApprovedNugetPackages },
        { "Conan", GetNotApprovedConanPackages },
        { "Debian", GetNotApprovedDebianPackages },
        { "Maven", GetNotApprovedMavenPackages },
        { "Poetry", GetNotApprovedPythonPackages }
    };

            if (unknownPackages.Any() && packageHandlers.TryGetValue(name, out var handler))
            {
                handler(unknownPackages, projectResponse, fileOperations, filepath, filename);
            }
        }
        public static void DisplayErrorForJfrogFoundPackages(List<ComponentsToArtifactory> jfrogFoundPackages)
        {
            if (jfrogFoundPackages.Any())
            {
                foreach (var jfrogFoundPackage in jfrogFoundPackages)
                {
                    switch (jfrogFoundPackage.ResponseMessage.ReasonPhrase)
                    {
                        case ApiConstant.ErrorInUpload:
                            Logger.Error($"Package {jfrogFoundPackage.Name}-{jfrogFoundPackage.Version} {jfrogFoundPackage.OperationType} Failed!! {jfrogFoundPackage.SrcRepoName} ---> {jfrogFoundPackage.DestRepoName}");
                            break;
                        case ApiConstant.PackageNotFound:
                            Logger.Error($"Package {jfrogFoundPackage.Name}-{jfrogFoundPackage.Version} not found in {jfrogFoundPackage.SrcRepoName}, Upload Failed!!");
                            break;
                        default:
                            Logger.Info($"Successful{jfrogFoundPackage.DryRunSuffix} {jfrogFoundPackage.OperationType} package {jfrogFoundPackage.Name}-{jfrogFoundPackage.Version} from {jfrogFoundPackage.SrcRepoName} to {jfrogFoundPackage.DestRepoName}");
                            break;
                    }
                }
                Logger.Info("\n");
            }
        }
        public static void DisplayErrorForJfrogPackages(List<ComponentsToArtifactory> jfrogNotFoundPackages)
        {
            if (jfrogNotFoundPackages.Any())
            {
                jfrogNotFoundPackages
                    .ForEach(pkg => Logger.Warn($"Package {pkg.Name}-{pkg.Version} is not found in jfrog"));

                Logger.Info("\n");
            }
        }
        private static void DisplayErrorForSucessfullPackages(List<ComponentsToArtifactory> successfulPackages)
        {
            if (successfulPackages.Any())
            {
                var packageMessages = successfulPackages
                    .Select(pkg => $"Package {pkg.Name}-{pkg.Version} is already uploaded")
                    .ToList();

                packageMessages.ForEach(Logger.Info);
                Logger.Info("\n");
            }
        }


        private static void GetNotApprovedPackages(List<ComponentsToArtifactory> unknownPackages, ProjectResponse projectResponse, IFileOperations fileOperations,
    string filepath,
    string filename,
    Func<ProjectResponse, List<JsonComponents>> getComponents,
    Action<ProjectResponse, List<JsonComponents>> setComponents)
        {
            if (File.Exists(filename))
            {
                string json = File.ReadAllText(filename);
                ProjectResponse myDeserializedClass = JsonConvert.DeserializeObject<ProjectResponse>(json);
                List<JsonComponents> components = new List<JsonComponents>();
                foreach (var package in unknownPackages)
                {
                    JsonComponents jsonComponents = new JsonComponents
                    {
                        Name = package.Name,
                        Version = package.Version
                    };
                    components.Add(jsonComponents);
                }
                setComponents(myDeserializedClass, components);
                fileOperations.WriteContentToReportNotApprovedFile(myDeserializedClass, filepath, FileConstant.artifactoryReportNotApproved, "Artifactory");
            }
            else
            {
                List<JsonComponents> components = new List<JsonComponents>();
                foreach (var package in unknownPackages)
                {
                    JsonComponents jsonComponents = new JsonComponents
                    {
                        Name = package.Name,
                        Version = package.Version
                    };
                    components.Add(jsonComponents);
                }
                setComponents(projectResponse, components);
                fileOperations.WriteContentToReportNotApprovedFile(projectResponse, filepath, FileConstant.artifactoryReportNotApproved, "Artifactory");
            }
            Logger.Warn($"Artifactory upload will not be done due to Report not in Approved state and package details can be found at {filename}\n");
        }

        private static void GetNotApprovedNpmPackages(
            List<ComponentsToArtifactory> unknownPackages,
            ProjectResponse projectResponse,
            IFileOperations fileOperations,
            string filepath,
            string filename)
        {
            GetNotApprovedPackages(unknownPackages, projectResponse, fileOperations, filepath, filename, pr => pr.Npm, (pr, components) => pr.Npm = components);
        }

        private static void GetNotApprovedNugetPackages(
            List<ComponentsToArtifactory> unknownPackages,
            ProjectResponse projectResponse,
            IFileOperations fileOperations,
            string filepath,
            string filename)
        {
            GetNotApprovedPackages(unknownPackages, projectResponse, fileOperations, filepath, filename, pr => pr.Nuget, (pr, components) => pr.Nuget = components);
        }

        private static void GetNotApprovedConanPackages(
            List<ComponentsToArtifactory> unknownPackages,
            ProjectResponse projectResponse,
            IFileOperations fileOperations,
            string filepath,
            string filename)
        {
            GetNotApprovedPackages(unknownPackages, projectResponse, fileOperations, filepath, filename, pr => pr.Conan, (pr, components) => pr.Conan = components);
        }

        private static void GetNotApprovedPythonPackages(
            List<ComponentsToArtifactory> unknownPackages,
            ProjectResponse projectResponse,
            IFileOperations fileOperations,
            string filepath,
            string filename)
        {
            GetNotApprovedPackages(unknownPackages, projectResponse, fileOperations, filepath, filename, pr => pr.Python, (pr, components) => pr.Python = components);
        }

        public static void GetNotApprovedDebianPackages(
            List<ComponentsToArtifactory> unknownPackages,
            ProjectResponse projectResponse,
            IFileOperations fileOperations,
            string filepath,
            string filename)
        {
            GetNotApprovedPackages(unknownPackages, projectResponse, fileOperations, filepath, filename, pr => pr.Debian, (pr, components) => pr.Debian = components);
        }

        private static void GetNotApprovedMavenPackages(List<ComponentsToArtifactory> unknownPackages, ProjectResponse projectResponse, IFileOperations fileOperations, string filepath, string filename)
        {
            GetNotApprovedPackages(unknownPackages, projectResponse, fileOperations, filepath, filename, pr => pr.Maven, (pr, components) => pr.Maven = components);
        }
        public static async Task AddUnknownPackagesAsync(Component item, DisplayPackagesInfo displayPackagesInfo)
        {
            string GetPropertyValue(string propertyName) =>
                item.Properties
                    .Find(p => p.Name == propertyName)?
                    .Value?
                    .ToUpperInvariant();

            var packageLists = new Dictionary<string, Action<ComponentsToArtifactory>>
    {
        { "NPM", components => displayPackagesInfo.UnknownPackagesNpm.Add(components) },
        { "NUGET", components => displayPackagesInfo.UnknownPackagesNuget.Add(components) },
        { "MAVEN", components => displayPackagesInfo.UnknownPackagesMaven.Add(components) },
        { "POETRY", components => displayPackagesInfo.UnknownPackagesPython.Add(components) },
        { "CONAN", components => displayPackagesInfo.UnknownPackagesConan.Add(components) },
        { "DEBIAN", components => displayPackagesInfo.UnknownPackagesDebian.Add(components) }
    };

            var projectType = GetPropertyValue(Dataconstant.Cdx_ProjectType);
            if (packageLists.TryGetValue(projectType, out var addComponent))
            {
                ComponentsToArtifactory components = await GetUnknownPackageinfo(item);
                addComponent(components);
            }
        }

        public static async Task JfrogNotFoundPackagesAsync(ComponentsToArtifactory item, DisplayPackagesInfo displayPackagesInfo)
        {
            var packageLists = new Dictionary<string, Action<ComponentsToArtifactory>>
    {
        { "NPM", components => displayPackagesInfo.JfrogNotFoundPackagesNpm.Add(components) },
        { "NUGET", components => displayPackagesInfo.JfrogNotFoundPackagesNuget.Add(components) },
        { "MAVEN", components => displayPackagesInfo.JfrogNotFoundPackagesMaven.Add(components) },
        { "POETRY", components => displayPackagesInfo.JfrogNotFoundPackagesPython.Add(components) },
        { "CONAN", components => displayPackagesInfo.JfrogNotFoundPackagesConan.Add(components) },
        { "DEBIAN", components => displayPackagesInfo.JfrogNotFoundPackagesDebian.Add(components) }
    };

            if (packageLists.TryGetValue(item.ComponentType, out var addComponent))
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                addComponent(components);
            }
        }

        public static async Task JfrogFoundPackagesAsync(ComponentsToArtifactory item, DisplayPackagesInfo displayPackagesInfo, string operationType, HttpResponseMessage responseMessage, string dryRunSuffix)
        {
            var packageLists = new Dictionary<string, Action<ComponentsToArtifactory>>
    {
        { "NPM", components => displayPackagesInfo.JfrogFoundPackagesNpm.Add(components) },
        { "NUGET", components => displayPackagesInfo.JfrogFoundPackagesNuget.Add(components) },
        { "MAVEN", components => displayPackagesInfo.JfrogFoundPackagesMaven.Add(components) },
        { "POETRY", components => displayPackagesInfo.JfrogFoundPackagesPython.Add(components) },
        { "CONAN", components => displayPackagesInfo.JfrogFoundPackagesConan.Add(components) },
        { "DEBIAN", components => displayPackagesInfo.JfrogFoundPackagesDebian.Add(components) }
    };

            if (packageLists.TryGetValue(item.ComponentType, out var addComponent))
            {
                ComponentsToArtifactory components = await GetPackageinfo(item, operationType, responseMessage, dryRunSuffix);
                addComponent(components);
            }
        }
        private static Task<ComponentsToArtifactory> GetUnknownPackageinfo(Component item)
        {

            ComponentsToArtifactory components = new ComponentsToArtifactory()
            {
                Name = item.Name,
                Version = item.Version
            };
            return Task.FromResult(components);

        }

        private static Task<ComponentsToArtifactory> GetPackageinfo(ComponentsToArtifactory item, string operationType, HttpResponseMessage responseMessage, string dryRunSuffix)
        {

            ComponentsToArtifactory components = new ComponentsToArtifactory()
            {
                Name = item.Name,
                Version = item.Version,
                SrcRepoName = item.SrcRepoName,
                DestRepoName = item.DestRepoName,
                OperationType = operationType,
                ResponseMessage = responseMessage,
                DryRunSuffix = dryRunSuffix,
                ComponentType = item.ComponentType,
                Purl = item.Purl,
                Token = item.Token,
                CopyPackageApiUrl = item.CopyPackageApiUrl,
                PackageName = item.PackageName,
                PackageType = item.PackageType,

            };
            return Task.FromResult(components);

        }
        public static async Task SucessfullPackagesAsync(ComponentsToArtifactory item, DisplayPackagesInfo displayPackagesInfo)
        {
            var successPackageLists = new Dictionary<string, Action<ComponentsToArtifactory>>
    {
        { "NPM", components => displayPackagesInfo.SuccessfullPackagesNpm.Add(components) },
        { "NUGET", components => displayPackagesInfo.SuccessfullPackagesNuget.Add(components) },
        { "MAVEN", components => displayPackagesInfo.SuccessfullPackagesMaven.Add(components) },
        { "POETRY", components => displayPackagesInfo.SuccessfullPackagesPython.Add(components) },
        { "CONAN", components => displayPackagesInfo.SuccessfullPackagesConan.Add(components) },
        { "DEBIAN", components => displayPackagesInfo.SuccessfullPackagesDebian.Add(components) }
    };

            if (successPackageLists.TryGetValue(item.ComponentType, out var addComponent))
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                addComponent(components);
            }
        }
        private static Task<ComponentsToArtifactory> GetSucessFulPackageinfo(ComponentsToArtifactory item)
        {

            ComponentsToArtifactory components = new ComponentsToArtifactory()
            {
                Name = item.Name,
                Version = item.Version,
                SrcRepoName = item.SrcRepoName,
                DestRepoName = item.DestRepoName,
                SrcRepoPathWithFullName = item.SrcRepoPathWithFullName,
                Path = item.Path,
                PackageType = item.PackageType,
                Purl = item.Purl,

            };
            return Task.FromResult(components);

        }

    }
}
