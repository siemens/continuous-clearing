using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.Common.Model
{   
    public class MultipleVersionValues
    {

        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }

    }

    public class MultipleVersions
    {
        public List<MultipleVersionValues> Multipleversions { get; set; }
    }
}
