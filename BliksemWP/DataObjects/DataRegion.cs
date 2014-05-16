using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BliksemWP.DataObjects
{
    /*
     * Store seperate regions of data (i.e NL, BE, CH)
     */
    [DataContract]
    class DataRegion
    {
        public DataRegion(string name)
        {
            NameShort = name;
        }

        [DataMember(Name = "shortName")]
        public string NameShort { get; set; }
        [DataMember(Name="longName")]
        public string NameLong { get; set; }

        [DataMember(Name = "stopsDb")]
        public string StopsDbLink { get; set; }

        [DataMember(Name = "timetable")]
        public string TimetableLink { get; set; }
        
        [DataMember(Name="validity")]
        public Validity DataValidity { get; set; }

        public string ValidityString
        {
            get
            {
                return "till " + this.DataValidity.DateTo.ToShortDateString();
            }
        }

        public Boolean IsActive
        {
            get
            {
                return (App.GetRegion() == this.NameShort);
            }
        }
    }
}
