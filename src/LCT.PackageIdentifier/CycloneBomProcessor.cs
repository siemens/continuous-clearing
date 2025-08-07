// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
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

            // Create metadata
            Metadata metadata = CreateMetadata(appSettings, projectReleases, caToolInformation);
            // Create definitions
            Definitions definitions = AddDefinitionsToBom();
            // Add metadata to BOM
            bom.Metadata = metadata;

            // Add definitions to BOM
            bom.Definitions = definitions;

            return bom;
        }

        private static Metadata CreateMetadata(CommonAppSettings appSettings, ProjectReleases projectReleases, CatoolInfo caToolInformation)
        {
            Metadata metadata = new Metadata
            {
                Tools = CreateToolChoices(caToolInformation),
                Properties = CreateProperties()
            };

            // Add metadata component
            metadata.Component = CreateMetadataComponent(appSettings, projectReleases);

            return metadata;
        }

        private static ToolChoices CreateToolChoices(CatoolInfo caToolInformation)
        {
            return new ToolChoices
            {
                Components = new List<Component>
        {
            new Component
            {
                Type= Component.Classification.Application,
                Supplier = new OrganizationalEntity
                {
                    Name = "Siemens AG"
                },
                Name = "Clearing Automation Tool",
                Version = caToolInformation.CatoolVersion,
                ExternalReferences = new List<ExternalReference>
                {
                    new ExternalReference
                    {
                        Type = ExternalReference.ExternalReferenceType.Website,
                        Url = Dataconstant.GithubUrl
                    }
                }
            }
        }
            };
        }

        private static List<Property> CreateProperties()
        {
            return new List<Property>
    {
        new Property
        {
            Name = "siemens:profile",
            Value = "clearing"
        }
    };
        }
        private static Component CreateMetadataComponent(CommonAppSettings appSettings, ProjectReleases projectReleases)
        {
            return new Component
            {
                Name = appSettings?.SW360?.ProjectName,
                Version = projectReleases.Version,
                Type = Component.Classification.Application
            };
        }
        private static Definitions AddDefinitionsToBom()
        {
            Definitions definitions = new Definitions
            {
                Standards = new List<Standard>
        {
            new Standard
            {
                Name = "Standard BOM",
                Version = "3.0.0",
                Description = "The Standard for Software Bills of Materials in Siemens",
                Owner = "Siemens AG",
                ExternalReferences = new List<ExternalReference>
                {
                    new ExternalReference
                    {
                        Type = ExternalReference.ExternalReferenceType.Website,
                        Url = Dataconstant.StandardSbomUrl
                    }
                },
                BomRef = "standard-bom"
            }
        }
            };

            return definitions;
        }
        public static void SetProperties(CommonAppSettings appSettings, Component component, ref List<Component> componentForBOM, string repo = "Not Found in JFrogRepo")
        {
            component.Properties ??= new List<Property>();
            var properties = component.Properties;
            CommonHelper.RemoveDuplicateAndAddProperty(ref properties, Dataconstant.Cdx_ProjectType, appSettings.ProjectType);
            CommonHelper.RemoveDuplicateAndAddProperty(ref properties, Dataconstant.Cdx_ArtifactoryRepoName, repo);
            CommonHelper.RemoveDuplicateAndAddProperty(ref properties, Dataconstant.Cdx_IsInternal, "false");
            CommonHelper.RemoveDuplicateAndAddProperty(ref properties, Dataconstant.Cdx_IsDevelopment, "false");
            CommonHelper.RemoveDuplicateAndAddProperty(ref properties, Dataconstant.Cdx_SiemensDirect, "true");
            CommonHelper.RemoveDuplicateAndAddProperty(ref properties, Dataconstant.Cdx_Siemensfilename, Dataconstant.PackageNameNotFoundInJfrog);
            CommonHelper.RemoveDuplicateAndAddProperty(ref properties, Dataconstant.Cdx_JfrogRepoPath, Dataconstant.JfrogRepoPathNotFound);
            component.Properties = properties;
            component.Description = null;
            componentForBOM.Add(component);
        }
    }
}