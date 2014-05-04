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

        public Journey()
        {
            Legs = new List<Leg>();
        }
    }
}
