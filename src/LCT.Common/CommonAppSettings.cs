// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.Common.Model;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace LCT.Common
{
    /// <summary>
    /// Common Application settings
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CommonAppSettings
    {
        private readonly IFolderAction folderAction;
        private readonly IFileOperations _fileOperations;

        public static string PackageUrlApi { get; set; } = $"https://www.nuget.org/api/v2/package/";
        public static string SourceURLNugetApi { get; set; } = $"https://api.nuget.org/v3-flatcontainer/";
        public static string SourceURLMavenApi { get; set; } = $"https://repo.maven.apache.org/maven2/";
        public static string SnapshotBaseURL { get; set; } = $"https://snapshot.debian.org/mr/";
        public static string SnapshotDownloadURL { get; set; } = $"https://snapshot.debian.org/archive/";
        public static string PyPiURL { get; set; } = $"https://pypi.org/pypi/";
        public static string SourceURLConan { get; set; } = "https://raw.githubusercontent.com/conan-io/conan-center-index/master/recipes/";
        public CommonAppSettings()
        {
            folderAction = new FolderAction();
            _fileOperations = new FileOperations();
        }

        public CommonAppSettings(IFolderAction iFolderAction)
        {
            folderAction = iFolderAction;
        }

        private string m_PackageFilePath;
        private string m_BomFolderPath;
        private string m_Sw360Token;
        private string m_SW360ProjectName;
        private string m_SW360ProjectID;
        private string m_ProjectType;
        private string m_ArtifactoryApiKey;
        private string m_SW360URL;
        private string m_BomFilePath;
        private string m_LogFolderPath;
        private string m_FOSSURL;
        private string m_ArtifactoryUser;
        private string m_CycloneDxSBomTemplatePath;


        public bool RemoveDevDependency { get; set; } = true;
        public string SW360AuthTokenType { get; set; } = "Bearer";
        public string JFrogApi { get; set; }
        public int TimeOut { get; set; } = 200;
        public Config Npm { get; set; }
        public Config Nuget { get; set; }

        public Config Maven { get; set; }
        public Config Debian { get; set; }
        public Config Python { get; set; }
        public Config Conan { get; set; }
        public string CaVersion { get; set; }
        public string CycloneDxSBomTemplatePath { get; set; }
        public string[] InternalRepoList { get; set; } = Array.Empty<string>();
        public bool EnableFossTrigger { get; set; } = true;
        public string JfrogNpmSrcRepo { get; set; }
        public string Mode { get; set; } = string.Empty;


        public string SW360URL
        {
            get
            {
                return m_SW360URL;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException($"Provide a sw360 url - {value}");
                }
                else
                {
                    m_SW360URL = value.TrimEnd(Dataconstant.ForwardSlash);
                }
            }
        }

        public string IdentifierBomFilePath
        {
            get
            {
                return m_BomFilePath;
            }
            set
            {
                if (!AppDomain.CurrentDomain.FriendlyName.Contains("SW360PackageCreator") &&
                    !AppDomain.CurrentDomain.FriendlyName.Contains("ArtifactoryUploader") &&
                    !string.IsNullOrEmpty(value))
                {
                    _fileOperations.ValidateFilePath(value);
                    m_BomFilePath = value;
                }
            }
        }

        public string PackageFilePath
        {
            get
            {
                return m_PackageFilePath;
            }
            set
            {
                if (!AppDomain.CurrentDomain.FriendlyName.Contains("SW360PackageCreator") &&
                    !AppDomain.CurrentDomain.FriendlyName.Contains("ArtifactoryUploader"))
                {
                    folderAction.ValidateFolderPath(value);
                    m_PackageFilePath = value;
                }
            }
        }

        public string LogFolderPath
        {
            get
            {
                return m_LogFolderPath;
            }
            set
            {
                folderAction.ValidateFolderPath(value);
                m_LogFolderPath = value;
            }
        }

        public string BomFolderPath
        {
            get
            {
                return m_BomFolderPath;
            }
            set
            {
                try
                {
                    m_BomFolderPath = value;
                    folderAction.ValidateFolderPath(value);
                }
                catch (DirectoryNotFoundException)
                {
                    Directory.CreateDirectory(m_BomFolderPath);
                }
            }
        }

        public string Sw360Token
        {
            get
            {
                return m_Sw360Token;
            }
            set
            {
                CommonHelper.CheckNullOrEmpty(nameof(Sw360Token), value);
                m_Sw360Token = value;
            }
        }

        public string SW360ProjectName
        {
            get
            {
                return m_SW360ProjectName;
            }
            set
            {
                CommonHelper.CheckNullOrEmpty(nameof(SW360ProjectName), value);
                m_SW360ProjectName = value;
            }
        }

        public string SW360ProjectID
        {
            get
            {
                return m_SW360ProjectID;
            }
            set
            {
                CommonHelper.CheckNullOrEmpty(nameof(SW360ProjectID), value);
                m_SW360ProjectID = value;
            }
        }

        public string ProjectType
        {
            get
            {
                return m_ProjectType;
            }
            set
            {
                CommonHelper.CheckNullOrEmpty(nameof(ProjectType), value);
                m_ProjectType = value;
            }
        }

        public string ArtifactoryUploadApiKey
        {
            get
            {
                return m_ArtifactoryApiKey;
            }
            set
            {
                CommonHelper.CheckNullOrEmpty(nameof(ArtifactoryUploadApiKey), value);
                m_ArtifactoryApiKey = value;
            }

        }

        public bool IsTestMode
        {
            get
            {
                return string.Compare(Mode, "test", true) == 0;
            }

        }

        public string Fossologyurl
        {
            get
            {
                return m_FOSSURL;
            }
            set
            {
                if (AppDomain.CurrentDomain.FriendlyName.Contains("SW360PackageCreator"))
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        throw new ArgumentNullException($"Provide a fossology url - {value}");
                    }
                    else
                    {
                        m_FOSSURL = value.TrimEnd(Dataconstant.ForwardSlash);
                    }
                }
            }
        }

        public string BomFilePath
        {
            get
            {
                return m_BomFilePath;
            }
            set
            {
                if (!AppDomain.CurrentDomain.FriendlyName.Contains("PackageIdentifier"))
                {
                    m_BomFilePath = value;
                    _fileOperations.ValidateFilePath(m_BomFilePath);
                }
            }
        }

        public string SBomTemplatePath
        {
            get
            {
                return m_CycloneDxSBomTemplatePath;
            }
            set
            {
                m_CycloneDxSBomTemplatePath = value;
                _fileOperations.ValidateFilePath(m_CycloneDxSBomTemplatePath);
            }
        }

        public string ArtifactoryUploadUser
        {
            get
            {
                return m_ArtifactoryUser;
            }
            set
            {
                CommonHelper.CheckNullOrEmpty(nameof(ArtifactoryUploadUser), value);
                m_ArtifactoryUser = value;
            }

        }

        public bool Release { get; set; } = false;

    }
}
