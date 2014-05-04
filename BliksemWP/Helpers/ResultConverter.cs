using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BliksemWP.DataObjects;
using BliksemWP.Enums;
using System.Globalization;

namespace BliksemWP.Helpers
{
    class ResultConverter
    {
        private List<Journey> journeys;
        
        public ResultConverter(string source)
        {
            journeys = new List<Journey>();
            var journeyStrings = source.Split(new[] { "===" }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();

            foreach (String journey in journeyStrings)
            {
                journeys.Add(GetJourney(journey));
            }
                       
        }

        private Journey GetJourney(string source)
        {
            Journey j = new Journey();

            var lines = source.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();

            for (int i = 0; i < lines.Count(); i++)
            {
                // Ignore the ITIN line
                if (i == 0) {
                    continue;
                }
                j.Legs.Add(GetJourneyLeg(lines[i]));
            }

            return j;
        }
        public List<Leg> GetLegs()
        {
            return journeys[0].Legs;
        }

        private static Leg GetJourneyLeg(string line)
        {
            Leg journeyLeg = new Leg();
            var columns = line.Split(new [] {";"}, StringSplitOptions.None);
            journeyLeg.JourneyType = (JourneyLegType)Enum.Parse(typeof(JourneyLegType), columns[0]);

            String format = "HH:mm:ss";
            try
            {
                journeyLeg.DepartureTime = DateTime.ParseExact(columns[5], format, CultureInfo.InvariantCulture);
                journeyLeg.ArrivalTime = DateTime.ParseExact(columns[6], format, CultureInfo.InvariantCulture);
            } catch (FormatException f) {
                Console.Write("Couldn't read " + columns[5], f);
            }

            journeyLeg.Departure = columns[12];
            journeyLeg.Arrival = columns[13];

            journeyLeg.Agency = columns[8];
            journeyLeg.Headsign = columns[10];
            journeyLeg.ProductName = columns[9];
            journeyLeg.ProductCategory = columns[11];

            return journeyLeg;
        }
    }


}
