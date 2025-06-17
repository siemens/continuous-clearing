// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.Common.Model;
using System;
using System.Collections.Generic;
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
        private readonly IFileOperations fileOperations;

        public static string PackageUrlApi { get; set; } = $"https://www.nuget.org/api/v2/package/";
        public static string SourceURLNugetApi { get; set; } = $"https://api.nuget.org/v3-flatcontainer/";
        public static string SourceURLMavenApi { get; set; } = $"https://repo.maven.apache.org/maven2/";
        public static string SnapshotBaseURL { get; set; } = $"https://snapshot.debian.org/mr/";
        public static string SnapshotDownloadURL { get; set; } = $"https://snapshot.debian.org/archive/";
        public static string PyPiURL { get; set; } = $"https://pypi.org/pypi/";
        public static string SourceURLConan { get; set; } = "https://raw.githubusercontent.com/conan-io/conan-center-index/master/recipes/";
        public static string AlpineAportsGitURL { get; set; } = $"https://gitlab.alpinelinux.org/alpine/aports.git";

        private string m_ProjectType;
        public CommonAppSettings()
        {
            folderAction = new FolderAction();
            fileOperations = new FileOperations();
            Directory = new Directory(folderAction, fileOperations);
        }

        public CommonAppSettings(IFolderAction iFolderAction, IFileOperations ifileOperations)
        {
            folderAction = iFolderAction;
            fileOperations = ifileOperations;
            Directory = new Directory(folderAction, fileOperations);
        }

        public int TimeOut { get; set; } = 200;
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
        public bool MultipleProjectType { get; set; } = false;
        public Telemetry Telemetry { get; set; }
        public SW360 SW360 { get; set; }
        public Directory Directory { get; set; }
        public Jfrog Jfrog { get; set; }
        public Config Npm { get; set; }
        public Config Nuget { get; set; }
        public Config Maven { get; set; }
        public Config Debian { get; set; }
        public Config Alpine { get; set; }
        public Config Poetry { get; set; }
        public Config Conan { get; set; }
        public string Mode { get; set; } = string.Empty;
        public bool IsTestMode
        {
            get
            {
                return string.Compare(Mode, "test", true) == 0;
            }

        }
    }
    public class Telemetry
    {
        public bool Enable { get; set; } = true;
        public string ApplicationInsightInstrumentKey { get; set; }
    }
    public class SW360
    {
        private string m_URL;
        private string m_Token;
        private string m_ProjectName;
        private string m_ProjectID;
        public string URL
        {
            get
            {
                return m_URL;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    m_URL = value.TrimEnd(Dataconstant.ForwardSlash);
                }
            }
        }
        public string ProjectName
        {
            get
            {
                return m_ProjectName;
            }
            set
            {
                m_ProjectName = value;
            }
        }
        public string ProjectID
        {
            get
            {
                return m_ProjectID;
            }
            set
            {
                m_ProjectID = value;
            }
        }
        public string AuthTokenType { get; set; } = "Bearer";
        public string Token
        {
            get
            {
                return m_Token;
            }
            set
            {
                m_Token = value;
            }
        }
        public Fossology Fossology { get; set; }
        public bool IgnoreDevDependency { get; set; } = true;
        public List<string> ExcludeComponents { get; set; }

    }
    public class Fossology
    {
        private string m_FOSSURL;
        public string URL
        {
            get
            {
                return m_FOSSURL;
            }
            set
            {
                if (!AppDomain.CurrentDomain.FriendlyName.Contains("PackageIdentifier") &&
                    !AppDomain.CurrentDomain.FriendlyName.Contains("ArtifactoryUploader"))
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        m_FOSSURL = value.TrimEnd(Dataconstant.ForwardSlash);
                    }
                }
            }
        }
        public bool EnableTrigger { get; set; }
    }

    public class Jfrog
    {
        private string m_Token;

        public string URL { get; set; }

        public string Token
        {
            get
            {
                return m_Token;
            }
            set
            {
                CommonHelper.CheckNullOrEmpty(nameof(Token), value);
                m_Token = value;
            }
        }

        public bool DryRun { get; set; } = false;
    }
    public class Directory
    {
        private readonly IFolderAction folderAction;
        private readonly IFileOperations fileOperations;
        private string m_InputFolder;
        private string m_OutputFolder;
        private string m_LogFolder;

        public Directory(IFolderAction folderAction, IFileOperations fileOperations)
        {
            this.folderAction = folderAction;
            this.fileOperations = fileOperations;
        }
        public string InputFolder
        {
            get
            {
                return m_InputFolder;
            }
            set
            {
                if (!AppDomain.CurrentDomain.FriendlyName.Contains("SW360PackageCreator") &&
                    !AppDomain.CurrentDomain.FriendlyName.Contains("ArtifactoryUploader"))
                {
                    folderAction.ValidateFolderPath(value);
                    m_InputFolder = value;
                }
            }
        }

        public string OutputFolder
        {
            get
            {
                return m_OutputFolder;
            }
            set
            {
                try
                {
                    m_OutputFolder = value;
                    folderAction.ValidateFolderPath(value);
                }
                catch (DirectoryNotFoundException)
                {
                    System.IO.Directory.CreateDirectory(m_OutputFolder);
                }
            }
        }

        public string LogFolder
        {
            get
            {
                return m_LogFolder;
            }
            set
            {
                try
                {
                    m_LogFolder = value;
                    folderAction.ValidateFolderPath(value);
                }
                catch (DirectoryNotFoundException)
                {
                    System.IO.Directory.CreateDirectory(m_LogFolder);
                }
            }
        }

    }

}
