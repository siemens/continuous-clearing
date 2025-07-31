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
    public static class SpdxSbomHelper
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static void CheckValidComponentsFromSpdxfile(Bom bom, string projectType,ref Bom listOfUnsupportedComponents)
        {
            Logger.Debug("CheckValidComponentsFromSpdxfile():Start identifying Supported and unsupported packages from spdx input files");
            List<Component> listUnsupportedComponents = new List<Component>();
            List<Dependency> listUnsupportedDependencies = new List<Dependency>();
            foreach (var component in bom.Components.ToList())
            {
                if (!string.IsNullOrEmpty(component.Name) && !string.IsNullOrEmpty(component.Version)
                    && !string.IsNullOrEmpty(component.Purl) &&
                    component.Purl.Contains(Dataconstant.PurlCheck()[projectType.ToUpper()]))
                {
                    //Taking Valid Components for perticular projects
                }
                else
                {
                    bom.Components.Remove(component);
                    listUnsupportedComponents.Add(component);
                    Logger.Debug($"CheckValidComponentsFromSpdxfile():Name:{component.Name},Version:{component.Version},Purl:{component.Purl} identified as a unsupported component");
                }
            }
            foreach (var dependency in bom.Dependencies.ToList())
            {
                if (string.IsNullOrEmpty(dependency.Ref) ||
                    !dependency.Ref.Contains(Dataconstant.PurlCheck()[projectType.ToUpper()]))
                {
                    bom.Dependencies.Remove(dependency);
                    listUnsupportedDependencies.Add(dependency);
                }
            }
            listOfUnsupportedComponents.Components.AddRange(listUnsupportedComponents);
            listOfUnsupportedComponents.Dependencies.AddRange(listUnsupportedDependencies);
            Logger.Debug($"CheckValidComponentsFromSpdxfile():Total identified unsupported Components:{listUnsupportedComponents.Count}");
            Logger.Debug($"CheckValidComponentsFromSpdxfile():Total identified unsupported Dependencies:{listUnsupportedDependencies.Count}");
            Logger.Debug("CheckValidComponentsFromSpdxfile():Completed the Supported and unsupported packages from spdx input files");
        }
        public static void AddSpdxPropertysForUnsupportedComponents(List<Component> UnsupportedComponentList, string filePath)
        {
            string filename = Path.GetFileName(filePath);
            foreach (var component in UnsupportedComponentList)
            {
                component.Properties ??= new List<Property>();
                AddSpdxComponentProperties(filename, component);
            }

        }
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
        public static void AddSpdxComponentProperties(string fileName, Component component)
        {
            component.Properties ??= new List<Property>();
            UpdateOrAddProperty(component.Properties, Dataconstant.Cdx_SpdxFileName, fileName);
            UpdateOrAddProperty(component.Properties, Dataconstant.Cdx_IdentifierType, Dataconstant.SpdxImport);
        }
        public static void AddDevelopmentPropertyForSpdx(bool devValue, Component component)
        {
            component.Properties ??= new List<Property>();
            UpdateOrAddProperty(component.Properties, Dataconstant.Cdx_IsDevelopment, devValue.ToString());
        }
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
    }
}
