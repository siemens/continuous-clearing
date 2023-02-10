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
            component.Description = string.Empty;
            List<Property> propList = new List<Property>();
            Property artifactoryrepo = new Property();
            Property projectType = new Property
            {
                Name = Dataconstant.Cdx_ProjectType,
                Value = appSettings.ProjectType
            };
            artifactoryrepo.Name = Dataconstant.Cdx_ArtifactoryRepoUrl;
            artifactoryrepo.Value = repo;
            Property internalType = new Property
            {
                Name = Dataconstant.Cdx_IsInternal,
                Value = "false"
            };
            propList.Add(internalType);
            propList.Add(artifactoryrepo);
            propList.Add(projectType);
            component.Properties = propList;
            componentForBOM.Add(component);
          
        }

    }
}
