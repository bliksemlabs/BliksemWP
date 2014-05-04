using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BliksemWP.Enums;

namespace BliksemWP.DataObjects
{
    public class Leg
    {
        public JourneyLegType LegType { get; set; }

        public String Agency { get; set; }        

        public string Departure { get; set; }
        public DateTime DepartureTime { get; set; }
        public string Arrival { get; set; }
        public DateTime ArrivalTime { get; set; }

        public String Headsign { get; set; }
        public String ProductCategory { get; set; }
        public String ProductName { get; set; }

        public String ProductDescription
        {
            get
            {
                if (LegType == JourneyLegType.WALK) {
                    return "Loop";
                } else if(LegType == JourneyLegType.WAIT) {
                    return "Overstap";
                } else {
                    return Agency + " " + ProductName + " naar " + Headsign;
                }
            }
        }

    }
}
