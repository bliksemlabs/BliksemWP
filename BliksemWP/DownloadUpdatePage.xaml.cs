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
        private int numberDownloadsDone = 0;
        private Boolean stopsSucceeded = false;
        private Boolean timetableSucceeded = false;

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
                if (e.Error == null && !e.Cancelled)
                {
                    ParseAndUpdateIndex(e.Result);
                    SaveFile(e, App.INDEX_FILE_NAME);
                    // Update last saved
                    IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
                    appSettings["indexLastUpdated"] = indexCheckDateValue;
                    appSettings.Save();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed to download stops");
                }
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

            HttpWebRequest webRequest;
            HttpWebResponse webResponse = null;
            try 
            {
                webRequest = (HttpWebRequest)asyncResult.AsyncState;
                webResponse = (HttpWebResponse)webRequest.EndGetResponse(asyncResult);
            }
            catch (WebException ex)
            {
                GetSavedFile();
            }
            
            String lastModified = webResponse.Headers[HttpRequestHeader.LastModified];
            indexCheckDateValue = DateTime.Parse(lastModified);

            if (indexCheckDateValue > getIndexLastUpdated())
            {
                DownloadNewIndex();
            }
            else
            {
                GetSavedFile();
            }
        }

        private void GetSavedFile()
        {
            IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication();
            try
            {
                using (var stream = iso.OpenFile(App.INDEX_FILE_NAME, FileMode.Open, FileAccess.Read))
                {
                    ParseAndUpdateIndex(stream);
                }
            }
            catch (FileNotFoundException e)
            {
                MessageBox.Show("Error getting file");
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
        
        private void btnApply_Click(object sender, RoutedEventArgs clickEvent)
        {
            startDownloadProgress();

            // Setup clients
            WebClient stopsClient = new WebClient();
            stopsClient.OpenReadCompleted += (s, e) =>
            {
                if (e.Error == null && !e.Cancelled)
                {
                    SaveFile(e, App.GetCurrentDataFilePath(App.STOPS_DB_NAME, currentSelected.NameShort, false));
                    stopsSucceeded = true;
                }
                numberDownloadsDone += 1;
                CheckComplete();
            };
            stopsClient.OpenReadAsync(new Uri(currentSelected.StopsDbLink));

            WebClient timetableClient = new WebClient();
            timetableClient.OpenReadCompleted += (s, e) =>
            {
                if (e.Error == null && !e.Cancelled)
                {
                    SaveFile(e, App.GetCurrentDataFilePath(App.DATA_FILE_NAME, currentSelected.NameShort, false));
                    timetableSucceeded = true;
                }
                numberDownloadsDone += 1;
                CheckComplete();
            };
            timetableClient.OpenReadAsync(new Uri(currentSelected.TimetableLink));
        }

        private void setNewRegion()
        {
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
            if (appSettings.Contains(App.KEY_REGION))
            {
                appSettings[App.KEY_REGION] = currentSelected.NameShort;
                appSettings[App.KEY_REGION_LONG] = currentSelected.NameLong;
            }
            else
            {
                appSettings.Add(App.KEY_REGION, currentSelected.NameShort);
                appSettings.Add(App.KEY_REGION_LONG, currentSelected.NameLong);
            }
            appSettings.Save();
        }

        private void CheckComplete()
        {
            if (numberDownloadsDone == 2)
            {
                endDownloadProgress();
                if (stopsSucceeded && timetableSucceeded)
                {
                    setNewRegion();
                }                
                NavigationService.GoBack();
            }
        }

        private void endDownloadProgress()
        {
            btnApply.Visibility = Visibility.Visible;
            progressPanel.Visibility = Visibility.Collapsed;
            numberDownloadsDone = 0;
            stopsSucceeded = true;
            timetableSucceeded = true;
        }
        private void startDownloadProgress()
        {
            btnApply.Visibility = Visibility.Collapsed;
            progressPanel.Visibility = Visibility.Visible;
            numberDownloadsDone = 0;
            stopsSucceeded = false;
            timetableSucceeded = false;
        }
    }
}