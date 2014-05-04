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
using Microsoft.Phone.Tasks;

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
            progressBar.Visibility = Visibility.Visible;
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
            progressBar.Visibility = Visibility.Collapsed;            
        }

        private void btnShare_Click(object sender, RoutedEventArgs e)
        {
            Button shareButton = (Button)sender;
            if (shareButton.DataContext is Journey)
            {
                Journey shareJourney = (Journey)shareButton.DataContext;
                ShareLinkTask shareLinkTask = new ShareLinkTask();
                shareLinkTask.Title = "Reisadvies";
                shareLinkTask.LinkUri = new Uri("http://1313.nl", UriKind.Absolute);
                shareLinkTask.Message = "Dit is een reisadvies met "+ shareJourney.Transfers + "overstappen";
                shareLinkTask.Show();
            }
        }

        private void btnAddCalendar_Click(object sender, RoutedEventArgs e)
        {
            Button calendarButton = (Button)sender;
            if (calendarButton.DataContext is Journey)
            {
                Journey calendarJourney = (Journey)calendarButton.DataContext;
                SaveAppointmentTask saveAppointmentTask = new SaveAppointmentTask();

                saveAppointmentTask.StartTime = calendarJourney.Legs[0].DepartureTime;
                saveAppointmentTask.EndTime = calendarJourney.Legs[calendarJourney.Legs.Count - 1].ArrivalTime;
                saveAppointmentTask.Subject = calendarJourney.Legs[0].Departure + " naar " + calendarJourney.Legs[calendarJourney.Legs.Count-1].Arrival;
                saveAppointmentTask.Location = calendarJourney.Legs[0].Departure;
                saveAppointmentTask.Details = "Reisadvies hier";
                saveAppointmentTask.IsAllDayEvent = false;
                saveAppointmentTask.Reminder = Reminder.FifteenMinutes;
                saveAppointmentTask.AppointmentStatus = Microsoft.Phone.UserData.AppointmentStatus.Busy;

                saveAppointmentTask.Show();
            }
        }
    }
}