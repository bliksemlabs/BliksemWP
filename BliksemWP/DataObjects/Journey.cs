using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BliksemWP.DataObjects
{
    class Journey
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
            TimeSpan duration = Legs[0].DepartureTime - Legs[Legs.Count - 1].ArrivalTime;
            return duration.ToString(@"hh\:mm")+" "+ Transfers + "x" ;
        }
    }
}
