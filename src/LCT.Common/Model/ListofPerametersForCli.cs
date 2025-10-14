using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Model
{
    [ExcludeFromCodeCoverage]
    public class ListofPerametersForCli
    {
        public string InternalRepoList { get; set; }
        public string Include { get; set; }
        public string Exclude { get; set; }
        public string ExcludeComponents { get; set; }
    }
}
