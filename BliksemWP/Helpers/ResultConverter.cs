using System;
using System.Collections.Generic;
using System.Linq;
using BliksemWP.DataObjects;
using BliksemWP.Enums;
using System.Globalization;

namespace BliksemWP.Helpers
{
    class ResultConverter
    {
        public JourneyList Journeys { get; private set; }

        public ResultConverter(string source)
        {
            Journeys = new JourneyList();
            var journeyStrings = SplitTrimString(source, "===");

            foreach (String journey in journeyStrings)
            {
                Journeys.Add(GetJourney(journey));
            }

        }

        private Journey GetJourney(string source)
        {
            Journey j = new Journey();

            var lines = SplitTrimString(source, "\n");

            for (int i = 0; i < lines.Count(); i++)
            {
                // Ignore the ITIN line
                if (i == 0)
                {
                    j.Transfers = Convert.ToInt32(lines[0].Split(' ')[1]);
                }
                else
                {
                    j.Legs.Add(GetJourneyLeg(lines[i]));
                }
            }

            return j;
        }

        private static List<string> SplitTrimString(string source, string delimeter)
        {
            return source.Split(new[] { delimeter }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
        }

        private static Leg GetJourneyLeg(string line)
        {
            Leg journeyLeg = new Leg();
            var columns = line.Split(new[] { ";" }, StringSplitOptions.None);
            journeyLeg.LegType = (JourneyLegType)Enum.Parse(typeof(JourneyLegType), columns[0]);

            String format = "HH:mm:ss";
            try
            {
                journeyLeg.DepartureTime = DateTime.ParseExact(columns[5], format, CultureInfo.InvariantCulture);
                // TODO - fix this round midnight - times are plus one day
                journeyLeg.ArrivalTime = DateTime.ParseExact(columns[6], format, CultureInfo.InvariantCulture);
            }
            catch (FormatException f)
            {
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