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
using System.Globalization;

namespace BliksemWP
{
    public partial class ResultPage : PhoneApplicationPage
    {
        private int fromStopId;
        private int toStopId;
        private DateTime requestTime = new DateTime();

        public ResultPage()
        {
            InitializeComponent();
            Loaded += ResultPage_Loaded;
        }

        void ResultPage_Loaded(object sender, RoutedEventArgs e)
        {
            String fromString, toString, dateString, timeString;
            progressBar.Visibility = Visibility.Visible;

            if (NavigationContext.QueryString.TryGetValue("from", out fromString))
            {
                fromStopId = Convert.ToInt32(fromString);
            }
            if (NavigationContext.QueryString.TryGetValue("to", out toString))
            {
                toStopId = Convert.ToInt32(toString);
            }
            if (NavigationContext.QueryString.TryGetValue("date", out dateString) && NavigationContext.QueryString.TryGetValue("time", out timeString))
            {
                requestTime = DateTime.Parse(dateString + " " + timeString);
            }

            var router = new NcxPppp.LibRrrr();
            long seconds = Convert.ToUInt32((requestTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds);
            var reisadvies = router.route(App.GetCurrentDataFilePath(App.DATA_FILE_NAME), fromStopId, toStopId, seconds, 0);
            // await async
            if (reisadvies.Length > 1)
            {
                ResultConverter c = new ResultConverter(reisadvies);
                PivotHolder.ItemsSource = c.Journeys; // Databinding does the rest
            }
            else
            {
                MessageBox.Show("Fout tijdens berekenen reisadvies. Probeer andere parameters");
                NavigationService.GoBack();
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
                shareLinkTask.LinkUri = new Uri(shareJourney.ToPlannerUrl(), UriKind.Absolute);
                shareLinkTask.Message = "Klik op de link om je reisadvies te zien op 1313.nl";
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
                saveAppointmentTask.Subject = calendarJourney.Legs[0].Departure + " naar " + calendarJourney.Legs[calendarJourney.Legs.Count - 1].Arrival;
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