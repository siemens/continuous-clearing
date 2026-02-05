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
    /// Provides equality comparison for Component objects based on Name, Version, and Purl.
    /// </summary>
    public class ComponentEqualityComparer : IEqualityComparer<Component>
    {
        #region Methods

        /// <summary>
        /// Determines whether two Component objects are equal.
        /// </summary>
        /// <param name="x">The first component to compare.</param>
        /// <param name="y">The second component to compare.</param>
        /// <returns>True if the components are equal; otherwise, false.</returns>
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

        /// <summary>
        /// Gets the hash code for the specified Component object.
        /// </summary>
        /// <param name="obj">The component to get the hash code for.</param>
        /// <returns>A hash code based on the component's Name, Version, and Purl.</returns>
        public int GetHashCode(Component obj)
        {
            return HashCode.Combine(obj.Name, obj.Version, obj.Purl);
        }

        #endregion
    }
}
