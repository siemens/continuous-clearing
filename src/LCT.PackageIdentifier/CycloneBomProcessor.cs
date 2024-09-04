// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Model;
using log4net;
using System.Collections.Generic;
using System.Reflection;

namespace LCT.PackageIdentifier
{
    public static class CycloneBomProcessor
    {

        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static Bom SetMetadataInComparisonBOM(Bom bom,
                                                     CommonAppSettings appSettings,
                                                     ProjectReleases projectReleases,
                                                     CatoolInfo caToolInformation)
        {
            Logger.Debug("Starting to add metadata info into the BOM");
            Metadata metadata = new Metadata
            {
                Tools = new List<Tool>(),
                Properties = new List<Property>()
            };

            SetMetaDataToolsValues(metadata, caToolInformation);

            Component component = new Component
            {
                Name = appSettings.SW360ProjectName,
                Version = projectReleases.Version,
                Type = Component.Classification.Application
            };
            metadata.Component = component;

            Property projectType = new Property
            {
                Name = "siemens:profile",
                Value = "clearing"
            };
            metadata.Properties.Add(projectType);

            bom.Metadata = metadata;
            return bom;
        }

        public static void SetMetaDataToolsValues(Metadata metadata, CatoolInfo caToolInformation)
        {
            Tool tool = new Tool
            {
                Name = "Clearing Automation Tool",
                Version = caToolInformation.CatoolVersion,
                Vendor = "Siemens AG",
                ExternalReferences = new List<ExternalReference>() { new ExternalReference { Url = "https://github.com/siemens/continuous-clearing", Type = ExternalReference.ExternalReferenceType.Website } }
            };
            metadata.Tools.Add(tool);

            Tool SiemensSBOM = new Tool
            {
                Name = "Siemens SBOM",
                Version = "2.0.0",
                Vendor = "Siemens AG",
                ExternalReferences = new List<ExternalReference>() { new ExternalReference { Url = "https://sbom.siemens.io/", Type = ExternalReference.ExternalReferenceType.Website } }
            };
            metadata.Tools.Add(SiemensSBOM);
        }
        public static void SetProperties(CommonAppSettings appSettings, Component component, ref List<Component> componentForBOM, string repo = "Not Found in JFrogRepo")
        {
            List<Property> propList = new();
            if (component.Properties?.Count == null || component.Properties.Count <= 0)
            {
                component.Properties = propList;
            }

            Property projectType = new()
            {
                Name = Dataconstant.Cdx_ProjectType,
                Value = appSettings.ProjectType
            };

            Property artifactoryrepo = new()
            {
                Name = Dataconstant.Cdx_ArtifactoryRepoName,
                Value = repo
            };

            Property internalType = new()
            {
                Name = Dataconstant.Cdx_IsInternal,
                Value = "false"
            };

            Property isDevelopment = new()
            {
                Name = Dataconstant.Cdx_IsDevelopment,
                Value = "false"
            };

            Property isDirect = new()
            {
                Name = Dataconstant.Cdx_SiemensDirect,
                Value = "true"
            };
            Property filname = new()
            {
                Name = Dataconstant.Cdx_Siemensfilename,
                Value = Dataconstant.PackageNameNotFoundInJfrog
            };
            Property jfrogRepoPathProperty = new()
            {
                Name = Dataconstant.Cdx_JfrogRepoPath,
                Value = Dataconstant.JfrogRepoPathNotFound
            };
            component.Properties.Add(internalType);
            component.Properties.Add(artifactoryrepo);
            component.Properties.Add(projectType);
            component.Properties.Add(isDevelopment);
            component.Properties.Add(isDirect);
            component.Properties.Add(filname);
            component.Properties.Add(jfrogRepoPathProperty);
            component.Description = null;
            componentForBOM.Add(component);
        }
    }
}