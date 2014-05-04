using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using BliksemWP.Controls;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BliksemWP.Helpers;
using BliksemWP.DataObjects;

namespace BliksemWP
{
    public partial class ResultPage : PhoneApplicationPage
    {
        private int fromStopId;
        private int toStopId;

        public ResultPage()
        {
            InitializeComponent();
            Loaded += ResultPage_Loaded;
        }

        void ResultPage_Loaded(object sender, RoutedEventArgs e)
        {
            String fromString, toString;
            if (NavigationContext.QueryString.TryGetValue("from", out fromString))
            {
                fromStopId = Convert.ToInt32(fromString);
            }
            if (NavigationContext.QueryString.TryGetValue("to", out toString))
            {
                toStopId = Convert.ToInt32(toString);
            } 
            
            var router = new NcxPppp.LibRrrr();
            var reisadvies = router.route(App.DATA_FILE_PATH, fromStopId, toStopId);
            if (reisadvies.Length > 1) {
                ResultConverter c = new ResultConverter(reisadvies);
                PivotHolder.ItemsSource = c.Journeys; // Databinding does the rest
            }
            
        }

        private void btnShare_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine(e.ToString());
        }

        private void btnAddCalendar_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine(e.ToString());
        }
    }
}