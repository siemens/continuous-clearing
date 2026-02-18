// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common.Constants;
using log4net;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LCT.Common
{
    /// <summary>
    /// Provides helper methods for working with SPDX SBOM files.
    /// </summary>
    public static class SpdxSbomHelper
    {
        #region Fields

        /// <summary>
        /// The logger instance for logging messages and errors.
        /// </summary>
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Fields

        #region Methods

        /// <summary>
        /// Checks and separates valid components from unsupported components in an SPDX file based on project type.
        /// </summary>
        /// <param name="bom">The BOM to validate.</param>
        /// <param name="projectType">The project type to validate against.</param>
        /// <param name="listOfUnsupportedComponents">The BOM to populate with unsupported components.</param>
        public static void CheckValidComponentsFromSpdxfile(Bom bom, string projectType, ref Bom listOfUnsupportedComponents)
        {
            Logger.Debug("CheckValidComponentsFromSpdxfile():Start identifying Supported and unsupported packages from spdx input files");
            
            // Filter unsupported components using LINQ
            var unsupportedComponents = bom.Components.Where(component =>
                string.IsNullOrEmpty(component.Name) || string.IsNullOrEmpty(component.Version) ||
                string.IsNullOrEmpty(component.Purl) ||
                !component.Purl.Contains(Dataconstant.PurlCheck()[projectType.ToUpper()])).ToList();

            // Log each unsupported component
            foreach (var component in unsupportedComponents)
            {
                Logger.DebugFormat("CheckValidComponentsFromSpdxfile():Name:{0},Version:{1},Purl:{2} identified as a unsupported component", component.Name, component.Version, component.Purl);
            }

            // Remove unsupported components from the BOM
            foreach (var component in unsupportedComponents)
            {
                bom.Components.Remove(component);
            }

            // Filter unsupported dependencies using LINQ
            var unsupportedDependencies = bom.Dependencies.Where(dependency =>
                string.IsNullOrEmpty(dependency.Ref) ||
                !dependency.Ref.Contains(Dataconstant.PurlCheck()[projectType.ToUpper()])).ToList();

            // Remove unsupported dependencies from the BOM
            foreach (var dependency in unsupportedDependencies)
            {
                bom.Dependencies.Remove(dependency);
            }

            listOfUnsupportedComponents.Components.AddRange(unsupportedComponents);
            listOfUnsupportedComponents.Dependencies.AddRange(unsupportedDependencies);
            Logger.DebugFormat("CheckValidComponentsFromSpdxfile():Total identified unsupported Components:{0}", unsupportedComponents.Count);
            Logger.DebugFormat("CheckValidComponentsFromSpdxfile():Total identified unsupported Dependencies:{0}", unsupportedDependencies.Count);
            Logger.Debug("CheckValidComponentsFromSpdxfile():Completed the Supported and unsupported packages from spdx input files");
        }

        /// <summary>
        /// Adds SPDX properties for unsupported components.
        /// </summary>
        /// <param name="UnsupportedComponentList">The list of unsupported components.</param>
        /// <param name="filePath">The file path of the SPDX file.</param>
        public static void AddSpdxPropertysForUnsupportedComponents(List<Component> UnsupportedComponentList, string filePath)
        {
            string filename = Path.GetFileName(filePath);
            foreach (var component in UnsupportedComponentList)
            {
                component.Properties ??= new List<Property>();
                AddSpdxComponentProperties(filename, component);
            }

        }

        /// <summary>
        /// Adds SPDX SBOM file name property to all components in the BOM.
        /// </summary>
        /// <param name="bom">The BOM to update.</param>
        /// <param name="filePath">The file path of the SPDX file.</param>
        public static void AddSpdxSBomFileNameProperty(ref Bom bom, string filePath)
        {
            if (bom?.Components != null)
            {
                string filename = Path.GetFileName(filePath);
                var bomComponentsList = bom.Components;
                foreach (var component in bomComponentsList)
                {
                    component.Properties ??= new List<Property>();
                    AddSpdxComponentProperties(filename, component);
                }
                bom.Components = bomComponentsList;
            }

        }

        /// <summary>
        /// Adds SPDX component properties including file name and identifier type.
        /// </summary>
        /// <param name="fileName">The SPDX file name.</param>
        /// <param name="component">The component to update.</param>
        public static void AddSpdxComponentProperties(string fileName, Component component)
        {
            component.Properties ??= new List<Property>();
            UpdateOrAddProperty(component.Properties, Dataconstant.Cdx_SpdxFileName, fileName);
            UpdateOrAddProperty(component.Properties, Dataconstant.Cdx_IdentifierType, Dataconstant.SpdxImport);
        }

        /// <summary>
        /// Adds or updates the development property for SPDX components.
        /// </summary>
        /// <param name="devValue">The development value to set.</param>
        /// <param name="component">The component to update.</param>
        public static void AddDevelopmentPropertyForSpdx(bool devValue, Component component)
        {
            component.Properties ??= new List<Property>();
            UpdateOrAddProperty(component.Properties, Dataconstant.Cdx_IsDevelopment, devValue.ToString());
        }

        /// <summary>
        /// Updates an existing property or adds a new property to the properties list.
        /// </summary>
        /// <param name="properties">The properties list to update.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="propertyValue">The value of the property.</param>
        private static void UpdateOrAddProperty(List<Property> properties, string propertyName, string propertyValue)
        {
            var existingProperty = properties.FirstOrDefault(p => p.Name == propertyName);
            if (existingProperty != null)
            {
                existingProperty.Value = propertyValue;
            }
            else
            {
                properties.Add(new Property { Name = propertyName, Value = propertyValue });
            }
        }

        /// <summary>
        /// Adds the development property to a component.
        /// </summary>
        /// <param name="component">The component to update.</param>
        /// <param name="isDevDependency">Whether the component is a development dependency.</param>
        public static void AddDevelopmentProperty(Component component, bool isDevDependency)
        {
            component.Properties ??= new List<Property>();

            // Add the property
            component.Properties.Add(new Property
            {
                Name = Dataconstant.Cdx_IsDevelopment,
                Value = isDevDependency ? "true" : "false"
            });
        }

        #endregion Methods
    }
}
