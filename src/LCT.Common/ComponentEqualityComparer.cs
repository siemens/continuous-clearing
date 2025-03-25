// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using System;
using System.Collections.Generic;

namespace LCT.Common
{
    /// <summary>
    ///  Comparer class
    /// </summary>
    public class ComponentEqualityComparer : IEqualityComparer<Component>
    {
        public bool Equals(Component x, Component y)
        {
            if (x == null && y == null)
                return true;
            else if (x == null || y == null)
                return false;
            else if ((x.Name == y.Name) && (x.Version == y.Version) && (x.Purl == y.Purl))
                return true;
            else
                return false;
        }

        public int GetHashCode(Component obj)
        {
            return HashCode.Combine(obj.Name, obj.Version, obj.Purl);
        }
    }
}
