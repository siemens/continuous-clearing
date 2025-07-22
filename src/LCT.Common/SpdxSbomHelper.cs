// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common.Constants;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LCT.Common
{
    public static class SpdxSbomHelper
    {
        public static void CheckValidComponentsFromSpdxfile(List<Component> bom, string projectType,ref List<Component> listOfUnsupportedComponents)
        {
            List<Component> listUnsupportedComponents = new List<Component>();
            foreach (var component in bom.ToList())
            {
                if (!string.IsNullOrEmpty(component.Name) && !string.IsNullOrEmpty(component.Version)
                    && !string.IsNullOrEmpty(component.Purl) &&
                    component.Purl.Contains(Dataconstant.PurlCheck()[projectType.ToUpper()]))
                {
                    //Taking Valid Components for perticular projects
                }
                else
                {
                    bom.Remove(component);
                    listUnsupportedComponents.Add(component);
                }
            }
            listOfUnsupportedComponents.AddRange(listUnsupportedComponents);
        }
        public static void AddSpdxPropertysForUnsupportedComponents(ref List<Component> UnsupportedComponentList, string filePath)
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
        public static void AddDevelopmentProperty(List<Component> componentsForBOM)
        {
            foreach (var component in componentsForBOM)
            {
                component.Properties ??= new List<Property>();
                UpdateOrAddProperty(component.Properties, Dataconstant.Cdx_IsDevelopment, "false");
            }
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
    }
}
