using System;
using BliksemWP.Enums;
using BliksemWP.Resources;

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
                    return AppResources.JourneyLegControl_Walk;
                }
                if (LegType == JourneyLegType.WAIT)
                {
                    //TODO: of wachten, als dit de eerste leg is?
                    return AppResources.JourneyLegControl_Change;
                }
                if (LegType == JourneyLegType.BUS)
                {
                    return string.Format("{0} {1}", AppResources.General_JourneyLegType_Bus, ProductName);
                }
                return ProductName;
            }
        }

        public bool DirectionVisible
        {
            get
            {
                return LegType != JourneyLegType.WALK && LegType != JourneyLegType.WAIT;
            }
        }

        public string Image
        {
            get { return LegType.ToString(); }
        }
    }
}
