using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using System.IO;
using System.Runtime.Serialization.Json;
using BliksemWP.DataObjects;

namespace BliksemWP
{
    public partial class DownloadUpdatePage : PhoneApplicationPage
    {
        private static readonly Uri INDEX_URL = new Uri("https://1313.nl/v3/index.json");
        // Store the value we're downloading to be able to save it
        private DateTime indexCheckDateValue;
        private DataRegion currentSelected;

        public DownloadUpdatePage()
        {
            InitializeComponent();
            Loaded += DownloadUpdatePage_Loaded;
        }

        void DownloadUpdatePage_Loaded(object sender, RoutedEventArgs e)
        {
            CheckNewerIndexVersionAvailable();
        }


        private void DownloadNewIndex()
        {
            WebClient indexClient = new WebClient();
            indexClient.OpenReadCompleted += (s, e) =>
            {
                ParseAndUpdateIndex(e.Result);
                SaveFile(e, App.INDEX_FILE_NAME);
                // Update last saved
                IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
                appSettings["indexLastUpdated"] = indexCheckDateValue;
                appSettings.Save();
            };
            indexClient.OpenReadAsync(INDEX_URL);
        }

        private void ParseAndUpdateIndex(Stream content)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(List<DataRegion>));
            List<DataRegion> regions = (List<DataRegion>)ser.ReadObject(content);
            Dispatcher.BeginInvoke(() =>
            {
                regionsList.ItemsSource = regions;
                LastUpdated.Text = getIndexLastUpdated().ToShortDateString();
            });
        }


        private void CheckNewerIndexVersionAvailable()
        {
            HttpWebRequest req = HttpWebRequest.CreateHttp(INDEX_URL);
            req.Method = "HEAD";
            req.BeginGetResponse(new AsyncCallback(VersionCheckResponseCallback), req);
        }

        private void VersionCheckResponseCallback(IAsyncResult asyncResult) {

            HttpWebRequest webRequest = (HttpWebRequest)asyncResult.AsyncState;
            HttpWebResponse webResponse = (HttpWebResponse)webRequest.EndGetResponse(asyncResult);

            String lastModified = webResponse.Headers[HttpRequestHeader.LastModified];
            indexCheckDateValue = DateTime.Parse(lastModified);

            if (indexCheckDateValue > getIndexLastUpdated())
            {
                DownloadNewIndex();
            }
            else
            {
                IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication();
                using (var stream = iso.OpenFile(App.INDEX_FILE_NAME, FileMode.Open, FileAccess.Read))
                {
                    ParseAndUpdateIndex(stream);
                }
            }
        }

        private static DateTime getIndexLastUpdated()
        {
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
            DateTime storeLastUpdated = DateTime.MinValue;
            if (appSettings.Contains("indexLastUpdated"))
            {
                storeLastUpdated = (DateTime)appSettings["indexLastUpdated"];
            }
            return storeLastUpdated;
        }

        void WebClient_OpenReadStopsCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            SaveFile(e, App.STOPS_DB_NAME);
        }

        private static void SaveFile(OpenReadCompletedEventArgs e, String filename)
        {
            var file = IsolatedStorageFile.GetUserStoreForApplication();
            e.Result.Position = 0;
            using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(filename, FileMode.Create, FileAccess.Write, file))
            {
                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                while ((bytesRead = e.Result.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, bytesRead);
                }
            }
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            RadioButton button = sender as RadioButton;
            if (button.DataContext is DataRegion)
            {
                currentSelected = (DataRegion)button.DataContext;
                btnApply.IsEnabled = true;
            }    
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}