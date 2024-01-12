// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace TestUtilities
{
    [ExcludeFromCodeCoverage]
    public class ComponentJsonParsor
    {
        public List< Component> Components { get; } = new List<Component>();
        public void Read(string path)
        {
            string json = File.ReadAllText(path);
            try
            {
                Bom components = JsonConvert.DeserializeObject<Bom>(json);

                foreach (var item in components.Components)
                {
                    Components.Add(item);
                }

            }
            catch (JsonReaderException)
            {
                // do nothing
            }
        }
    }
}
