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

        public string Agency { get; set; }

        public string Departure { get; set; }
        public DateTime DepartureTime { get; set; }
        public string Arrival { get; set; }
        public DateTime ArrivalTime { get; set; }

        public string Headsign { get; set; }
        public string ProductCategory { get; set; }
        public string ProductName { get; set; }

        public string ProductDescription
        {
            get
            {
                if (LegType == JourneyLegType.WALK)
                {
                    return "Loop";
                }
                else if (LegType == JourneyLegType.WAIT)
                {
                    return "Overstap";
                }
                else
                {
                    return Agency + " " + ProductName + " naar " + Headsign;
                }
            }
        }

        public string Image
        {
            get { return LegType.ToString(); }
        }

    }
}
