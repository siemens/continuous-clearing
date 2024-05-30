// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common;
using LCT.Common.Constants;
using log4net;
using System.Collections.Generic;
using System.Reflection;
using static CycloneDX.Models.ExternalReference;

namespace LCT.PackageIdentifier
{
    public static class CycloneBomProcessor
    {

        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static Bom SetMetadataInComparisonBOM(Bom bom, CommonAppSettings appSettings)
        {
            Logger.Debug("Starting to add metadata info into the BOM");

            List<Tool> tools = new List<Tool>();
            Tool tool = new Tool
            {
                Name = "Clearing Automation Tool",
                Version = appSettings.CaVersion,
                Vendor = "Siemens AG"
            };
            tools.Add(tool);

            if (bom.Metadata != null)
            {
                bom.Metadata.Tools.AddRange(tools);
            }
            else
            {
                bom.Metadata = new Metadata
                {
                    Tools = tools
                };
            }
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
            component.Description = string.Empty;
            componentForBOM.Add(component);
        }
    }
}