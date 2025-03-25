// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace UnitTestUtilities
{
    /// <summary>
    /// The UTConstant class
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class UTParams
    {
        public static readonly IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettingsUnitTest.json", true, true).Build();

        public static readonly string SW360URL = config["SW360URL"];
        public static readonly string FossologyURL = config["FossologyURL"];
        public static readonly string JFrogURL = config["JFrogURL"];

    }
}
