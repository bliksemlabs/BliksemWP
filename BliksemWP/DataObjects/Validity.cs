using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BliksemWP.DataObjects
{
    [DataContract]
    class Validity
    {
        [DataMember(Name="from")]
        public string From { get; set; }
        public DateTime DateFrom
        {
            get
            {
                return DateTime.Parse(From);
            }
        }
        [DataMember(Name = "to")]
        public string To { get; set; }
        public DateTime DateTo
        {
            get
            {
                return DateTime.Parse(To);
            }
        }
    }
}
