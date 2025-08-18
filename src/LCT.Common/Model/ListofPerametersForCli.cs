using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
