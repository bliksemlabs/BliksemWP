using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using BliksemWP.Helpers;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BliksemWP.Resources;
using System.IO.IsolatedStorage;
using Windows.Storage;
using SQLiteWinRT;
using System.Collections;
using System.Diagnostics;

namespace BliksemWP
{
    public partial class MainPage : PhoneApplicationPage
    {
        Database db;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            LayoutRoot.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            SetupDB();
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            LayoutRoot.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            LayoutRoot.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        async void SetupDB()
        {
            // Get the file from the install location  
            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("stops.db");

            // Create a new SQLite instance for the file 
            this.db = new Database(file);

            // Open the database asynchronously
            await this.db.OpenAsync(SqliteOpenMode.OpenRead);
        }

        void btnStart_Click(object sender, RoutedEventArgs e)
        {
            Stop fromStop = (Stop) from.SelectedItem;
            Stop toStop = (Stop) to.SelectedItem;
            if (fromStop != null && toStop != null) {
                NavigationService.Navigate(new Uri("/ResultPage.xaml?from=" + fromStop.StopIndex + "&to=" + toStop.StopIndex + 
                    "&date="+ datePicker.ValueString + "&time="+timePicker.ValueString, UriKind.Relative));
            }
            else
            {
                MessageBox.Show("Kies eerst twee stations");
            }
            
        }

        void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/DownloadUpdatePage.xaml", UriKind.Relative));
        }

        /// <summary>
        /// Full Text Searches sqlite db for stops name to autocomplete
        /// </summary>
        /// <param name="searchText">text to search</param>
        /// <param name="sender">target AutoCompleteBox</param>
        async void SearchForStop(string searchText, object sender)
        {
            List<Stop> items = new List<Stop>();

            // Prepare a SQL statement to be executed
            // RANK? ORDER BY rank(matchinfo(stops_fts)) DESC 
            var statement = await this.db.PrepareStatementAsync("SELECT stopindex, stopname FROM stops_fts WHERE stopname MATCH ? LIMIT 50 OFFSET 0;");
            string formattedText = "*" + searchText.Trim().Replace(" ", "* *") + "*";
            statement.BindTextParameterAt(1, formattedText);

            // Loop through all the results and add to the collection
            while (await statement.StepAsync())
            {
                Stop stop = new Stop() { StopIndex = statement.GetIntAt(0), StopName = statement.GetTextAt(1)  };
                items.Add(stop);
            }

            Debug.WriteLine(formattedText + ": "+ items.Count);
            (sender as AutoCompleteBox).ItemsSource = items;
            (sender as AutoCompleteBox).PopulateComplete();
        }

        /// <summary>
        /// Event triggered by typing from user
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="e">event arguments</param>
        private void Stops_Populating(object sender, PopulatingEventArgs e)
        {
            if (this.db == null)
            {
                // cancel populating
                e.Cancel = true;
            }

            SearchForStop(e.Parameter, sender);

            e.Cancel = true;
        }


       
    }
}