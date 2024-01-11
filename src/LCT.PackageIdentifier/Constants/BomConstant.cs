// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Constants
{
    /// <summary>
    /// Bom Constants
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class BomConstant
    {
        public const string Log4netBomCreatorConfigFileName = "log4netPackageIdentifier.config";
        public const string PackageLockFileName = "package-lock.json";
        public const string PackageConfigFileName = "packages.config";
        public const string PackageLockJsonFileName = "packages.lock.json";
        public const int MaxDegreeOfParallelism = 2;
    }
}
