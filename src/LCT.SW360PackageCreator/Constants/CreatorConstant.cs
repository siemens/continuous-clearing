// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.SW360PackageCreator.Constants
{
    /// <summary>
    /// Creaates the constant
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class CreatorConstant
    {
        public const string Log4NetCreatorConfigFileName = "log4netComponentCreator.config";
        public const int MaxDegreeOfParallelism = 2;
    }
}
