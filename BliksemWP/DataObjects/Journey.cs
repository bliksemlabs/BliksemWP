using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BliksemWP.DataObjects
{
    public class Journey
    {
        public List<Leg> Legs { get; set; }

        public int Transfers { get; set; }

        public Journey()
        {
            Transfers = 0;
            Legs = new List<Leg>();
        }

        public override string ToString()
        {
            //TimeSpan duration = Legs[0].DepartureTime - Legs[Legs.Count - 1].ArrivalTime;
            //return duration.ToString(@"hh\:mm")+" "+ Transfers + "x" ;
            return string.Format("{0:t} {1}x", Legs[0].DepartureTime, Transfers);
        }

        public string ToPlannerUrl()
        {
            //i.e #fromPlace=Rotterdam%2C+Centraal+Station&fromLatLng=51.922375%2C4.471512&toPlace=Groningen+Europapark&toLatLng=53.20623%2C6.582901&time=13%3A17&date=2014-05-25&arriveBy=false
            StringBuilder s = new StringBuilder();
            s.Append("https://1313.nl/#fromPlace=");
            s.Append(Legs[0].Departure);
            s.Append("&fromLatLng=");
            s.Append(Legs[0].DepartureLatitude);
            s.Append("%2C");
            s.Append(Legs[0].DepartureLongitude);
            int lastIndex = Legs.Count - 1;
            s.Append("&toPlace=");
            s.Append(Legs[0].Arrival);
            s.Append("&toLatLng=");
            s.Append(Legs[lastIndex].ArrivalLatitude);
            s.Append("%2C");
            s.Append(Legs[lastIndex].ArrivalLongitude);
            return s.ToString();
        }
    }
}
