using LCT.Common.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.Common.Logging
{
    public static class LegendMappings
    {
        private static readonly Dictionary<string, string> IdentifierLegendColorMap = new(StringComparer.OrdinalIgnoreCase)
{            
    { "Input File", "blue" },
    { "Total Invalid/Duplicate/Excluded", "magenta" },
    { "Dependent Components", "yellow" },
    { "Internal Components", "cyan" },
    { "Repos", "teal" },
    { "SBOM", "purple" }
};
        private static readonly (string[] Patterns, string LegendLabel)[] IdentifierTableKeyToLegend = new[]
        {    
    (new[] {
        "Components already present in 3rd party repo(s)",
        "Components already present in devdep repo(s)",
        "Components already present in release repo(s)"
    }, "Repos"),

    (new[] {"Internal Components Identified"}, "Internal Components"),
    (new[] { "Components In Input File" }, "Input File"),

    (new[] { "Dev Dependent Components", "Bundled Dependent Components" }, "Dependent Components"),

    (new[] {
        "Total InvalidComponents Excluded",
        "Total Duplicate Components",
        "Total Components Excluded SW360"
    }, "Total Invalid/Duplicate/Excluded"),

    (new[] {
        "Components Added From SBOM Template",
        "Components Updated From SBOM Template",
        "Total SPDX components imported as baseline entries",
        "Components In Comparison BOM"
    }, "SBOM"),
};
        private static readonly Dictionary<string, string> UploaderLegendColorMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Input file", "blue" },
            { "Packages in approved/NotApproved", "yellow" },
            { "Siparty DevDep", "magenta" },
            { "Internal Packages", "cyan" },
            { "Packages Not Existing/Not Actioned", "teal" }
        };

        private static readonly (string[] Patterns, string LegendLabel)[] UploaderTableKeyToLegend = new[]
        {
            (new[] { "Components in Comparison BOM" }, "Input file"),
            (new[] { "Packages in Not Approved State", "Packages in Approved State" }, "Packages in approved/NotApproved"),
            (new[] {
                "Development Packages to be Copied to Siparty DevDep Repo",
                "Development Packages Copied to Siparty DevDep Repo",
                "Development Packages Not Copied to Siparty DevDep Repo"
            }, "Siparty DevDep"),
            (new[] {
                "Internal Packages to be Moved",
                "Internal Packages Moved to Repo",
                "Internal Packages Not Moved to Repo"
            }, "Internal Packages"),
            (new[] {
                "Packages Not Copied to Siparty Repo",
                "Packages Copied to Siparty Repo",
                "Packages Not Existing in Repository",
                "Packages Not Actioned Due To Error"
            }, "Packages Not Existing/Not Actioned"),
        };

        private static readonly Dictionary<string, string> CreatorLegendColorMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Components read from Comparison BOM", "blue" },
            { "Components and releases (created, exists, with/without URLs)", "green" },
            { "Components uploaded/not uploaded in FOSSology", "magenta" },
            { "Total Duplicate and InValid Components", "cyan" },
            { "Components or releases not created in SW360", "teal" }
        };

        private static readonly (string[] Patterns, string LegendLabel)[] CreatorTableKeyToLegend = new[]
        {
            (new[] { "Components read from Comparison BOM" }, "Components read from Comparison BOM"),
            (new[] {
                "Components or releases created newly in SW360",
                "Components or releases exists in SW360",
                "Components without source download URL",
                "Components with source download URL",
                "Components without package URL",
                "Components without source and package URL",
                "Components or releases not created in SW360"
            }, "Components and releases (created, exists, with/without URLs)"),
            (new[] {
                "Components uploaded in FOSSology",
                "Components not uploaded in FOSSology"
            }, "Components uploaded/not uploaded in FOSSology"),
            (new[] { "Total Duplicate and InValid Components" }, "Total Duplicate and InValid Components"),
        };
        public static Dictionary<string, string> GetLegendColorMap(string exeTypeNormalized)
        {
            return exeTypeNormalized switch
            {
                Dataconstant.Identifier => IdentifierLegendColorMap,
                Dataconstant.Creator => CreatorLegendColorMap,
                Dataconstant.Uploader => UploaderLegendColorMap,
                _ => IdentifierLegendColorMap
            };
        }

        public static (string[] Patterns, string LegendLabel)[] GetTableKeyToLegend(string exeTypeNormalized)
        {
            return exeTypeNormalized switch
            {
                Dataconstant.Identifier => IdentifierTableKeyToLegend,
                Dataconstant.Creator => CreatorTableKeyToLegend,
                Dataconstant.Uploader => UploaderTableKeyToLegend,
                _ => IdentifierTableKeyToLegend
            };
        }
        public static string[] GetLegendRow(string exeTypeNormalized)
        {
            return exeTypeNormalized switch
            {
                Dataconstant.Identifier => new[]
                {
            $"[blue]■[/] Input File",
            $"[magenta]■[/] Total Invalid/Duplicate/Excluded",
            $"[yellow]■[/] Dependent Components",
            $"[cyan]■[/] Internal Components",
            $"[teal]■[/] Repos",
            $"[purple]■[/] SBOM"
        },
                Dataconstant.Creator => new[]
                {
            $"[blue]■[/] Components read from Comparison BOM",
            $"[green]■[/] Components and releases (created, exists, with/without URLs)",
            $"[magenta]■[/] Components uploaded/not uploaded in FOSSology",
            $"[cyan]■[/] Total Duplicate and InValid Components"
        },
                _=> new[]
                {
            $"[blue]■[/] Input file",
            $"[yellow]■[/] Packages in approved/NotApproved",
            $"[magenta]■[/] Siparty DevDep",
            $"[cyan]■[/] Internal Packages",
            $"[teal]■[/] Packages Not Existing/Not Actioned"
        }
            };
        }
    }
}
