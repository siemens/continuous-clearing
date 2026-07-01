// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace SIT.Create.Constants
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
