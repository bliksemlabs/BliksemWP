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

            try
            {
                journeyLeg.DepartureTime = parseDateFromString(columns[5]);
                journeyLeg.ArrivalTime = parseDateFromString(columns[6]);
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

            journeyLeg.DepartureLatitude = columns[14];
            journeyLeg.DepartureLongitude = columns[15];

            journeyLeg.ArrivalLatitude = columns[16];
            journeyLeg.ArrivalLongitude = columns[17];

            return journeyLeg;
        }

        private static DateTime parseDateFromString(String text)
        {
            DateTime parsed = DateTime.ParseExact(text.Replace(" +1D", ""), "HH:mm:ss", CultureInfo.InvariantCulture);
            if (text.Contains(" +1D")) {
                parsed.AddDays(1);
            }
            return parsed;
        }
    }
}