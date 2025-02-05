// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator.Model
{
    public class ReleasesAllDetails
    {
        [ExcludeFromCodeCoverage]

        public class Embedded
        {
            [JsonProperty("sw360:releases")]
            public List<Sw360Release> sw360releases { get; set; }

            [JsonProperty("sw360:attachments")]
            public List<List<Attachment>> sw360attachments { get; set; }
           
        }
        public class Attachment
        {
             public string filename { get; set; }
    
        }
       

        

        public class Links
        {
           
            public Self self { get; set; }
           
           
        }        
       

        public Embedded _embedded { get; set; }
       
        public Page page { get; set; }

        public class Self
        {
            public string href { get; set; }
        }        

        public class Sw360Component
        {
            public string href { get; set; }
        }       

        public class Sw360Release
        {
            public string name { get; set; }
            public string version { get; set; }          
           
            public string clearingState { get; set; }            
            
            public Links _links { get; set; }
            public Embedded _embedded { get; set; }
            
           
        }

        public class Page
        {
           
            public int totalPages { get; set; }
           
        }
    }
}
