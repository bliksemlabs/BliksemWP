using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BliksemWP.DataObjects
{
    class JourneyList : ObservableCollection<Journey>
    {
        public JourneyList() : base()
        {

        }
    }
}
