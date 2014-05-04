﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BliksemWP.DataObjects
{
    /*
     * Store seperate regions of data (i.e NL, BE, CH)
     */
    class DataRegion
    {
        public string CountryLong { get; set; }
        public string CountryShort { get; set; }

        public DateTime LastUpdate { get; set; }
    }
}
