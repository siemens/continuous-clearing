// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common;
using LCT.Common.Constants;
using log4net;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LCT.PackageIdentifier
{
    public static class CycloneBomProcessor
    {

        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static Bom SetMetadataInComparisonBOM(Bom bom,CommonAppSettings appSettings)
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

        public static void SetProperties(CommonAppSettings appSettings, Component component,ref List<Component> componentForBOM, string repo = "Not Found in JFrogRepo")
        {
            List<Property> propList = new();

            if (component.Properties?.Count == null || component.Properties.Count <= 0)
            {
                component.Properties = propList;

            }

            bool res = component.Properties.Any(x => x.Name.Equals(Dataconstant.Cdx_IsInternal));

            if (res)
            {
                //do nothing
            }
            else
            {
                Property internalType = new()
                {
                    Name = Dataconstant.Cdx_IsInternal,
                    Value = "false"
                };
                component.Properties.Add(internalType);

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

            component.Properties.Add(artifactoryrepo);
            component.Properties.Add(projectType);
            component.Description = string.Empty;
            componentForBOM.Add(component);

        }

    }
}
