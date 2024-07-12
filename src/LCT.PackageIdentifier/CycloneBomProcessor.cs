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
using NuGet.Packaging.Signing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.Policy;
using static CycloneDX.Models.ExternalReference;

namespace LCT.PackageIdentifier
{
    public static class CycloneBomProcessor
    {

        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static Bom SetMetadataInComparisonBOM(Bom bom, CommonAppSettings appSettings, ProjectReleases projectReleases,CatoolInfo caToolInformation)
        {
            Logger.Debug("Starting to add metadata info into the BOM");
            List<Tool> tools = new List<Tool>();
            List<Component> components = new List<Component>();
            List<Property> properties = new();
            Tool tool = new Tool
            {
                Name = "Clearing Automation Tool",
                Version = caToolInformation.CatoolVersion,
                Vendor = "Siemens AG",
                ExternalReferences = new List<ExternalReference>() { new ExternalReference { Url = "https://github.com/siemens/continuous-clearing", Type = ExternalReference.ExternalReferenceType.Website } }

            };
            tools.Add(tool);
            Tool SiemensSBOM = new Tool
            {
                Name = "Siemens SBOM",
                Version = "2.0.0",
                Vendor = "Siemens AG",
                ExternalReferences = new List<ExternalReference>() { new ExternalReference { Url = "https://sbom.siemens.io/", Type = ExternalReference.ExternalReferenceType.Website } }
            };
            tools.Add(SiemensSBOM);
            Component component = new Component
            {
                Name = appSettings.SW360ProjectName,
                Version = projectReleases.Version,
                Type = Component.Classification.Application
            };
            components.Add(component);            

            if (bom.Metadata != null)
            {
                bom.Metadata.Tools.AddRange(tools);
                bom.Metadata.Component.Name = component.Name;
                bom.Metadata.Component.Version = component.Version;
                bom.Metadata.Component.Type = component.Type;
            }
            else
            {
                bom.Metadata = new Metadata
                {
                    Tools = tools,
                    Component = component,
                };
            }
            Property projectType = new()
            {
                Name = "siemens:profile",
                Value = "clearing"
            };
            bom.Metadata.Properties = properties;            
            bom.Metadata.Properties.Add(projectType);
            return bom;
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
                Name = Dataconstant.Cdx_ArtifactoryRepoUrl,
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

            component.Properties.Add(internalType);
            component.Properties.Add(artifactoryrepo);
            component.Properties.Add(projectType);
            component.Properties.Add(isDevelopment);
            component.Description = null;
            componentForBOM.Add(component);
        }
    }
}