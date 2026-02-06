// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace TestUtilities
{
    [ExcludeFromCodeCoverage]
    public class ComponentJsonParsor
    {
        public List<Component> Components { get; } = new List<Component>();
        public Bom BoM { get; } = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
        public void Read(string path)
        {
            var json = File.ReadAllText(path);
            try
            {
                Bom components = CycloneDX.Json.Serializer.Deserialize(json);

                foreach (var item in components.Components)
                {
                    Components.Add(item);
                    BoM.Components.Add(item);
                }
                foreach (var item in components.Dependencies)
                {
                    BoM.Dependencies.Add(item);
                }

            }
            catch (JsonReaderException)
            {
                // do nothing
            }
        }

    }
}
