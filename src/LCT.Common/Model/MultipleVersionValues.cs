using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.Common.Model
{   
    public class MultipleVersionValues
    {        
        public string ComponentName { get; set; }        
        public string ComponentVersion { get; set; }        
        public string PackageFoundIn { get; set; }

    }

    public class MultipleVersions
    {
        public List<MultipleVersionValues> Npm { get; set; }
        public List<MultipleVersionValues> Nuget { get; set; }
        public List<MultipleVersionValues> Conan { get; set; }
    }
}
