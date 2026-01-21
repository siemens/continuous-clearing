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

        #region Fields
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        #endregion

        #region Properties
        #endregion

        #region Constructors
        #endregion

        #region Methods
        /// <summary>
        /// Adds metadata and definitions to the comparison BOM.
        /// </summary>
        /// <param name="bom">BOM to update.</param>
        /// <param name="appSettings">Application settings used to populate metadata.</param>
        /// <param name="projectReleases">Project releases used for metadata versioning.</param>
        /// <param name="caToolInformation">CA tool information for tool metadata.</param>
        /// <returns>The updated BOM containing metadata and definitions.</returns>
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

        /// <summary>
        /// Creates the CycloneDX metadata object including tools, properties and component.
        /// </summary>
        /// <param name="appSettings">Application settings used to populate metadata component.</param>
        /// <param name="projectReleases">Project release information used for version metadata.</param>
        /// <param name="caToolInformation">CA tool information used to describe the tool.</param>
        /// <returns>A populated Metadata instance.</returns>
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

        /// <summary>
        /// Builds the tool choices section describing the CA tool used to create the BOM.
        /// </summary>
        /// <param name="caToolInformation">CA tool information providing version details.</param>
        /// <returns>ToolChoices containing tool component information.</returns>
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

        /// <summary>
        /// Creates a list of default properties included in metadata.
        /// </summary>
        /// <returns>List of property objects for metadata.</returns>
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

        /// <summary>
        /// Creates the metadata component entry for the BOM using SW360 project info and release version.
        /// </summary>
        /// <param name="appSettings">Application settings containing SW360 project information.</param>
        /// <param name="projectReleases">Project release information containing a version.</param>
        /// <returns>A Component instance used in metadata.</returns>
        private static Component CreateMetadataComponent(CommonAppSettings appSettings, ProjectReleases projectReleases)
        {
            return new Component
            {
                Name = appSettings?.SW360?.ProjectName,
                Version = projectReleases.Version,
                Type = Component.Classification.Application
            };
        }

        /// <summary>
        /// Constructs definitions used in the BOM such as standards and their metadata.
        /// </summary>
        /// <returns>Definitions object populated with standard information.</returns>
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

        /// <summary>
        /// Sets a collection of standard properties on a component and adds it to the output list.
        /// </summary>
        /// <param name="appSettings">Application settings to determine project type property.</param>
        /// <param name="component">Component to update with properties.</param>
        /// <param name="componentForBOM">Reference list where the component will be added.</param>
        /// <param name="repo">Optional repository name to set as a property.</param>
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
        #endregion

        #region Events
        #endregion
    }
}