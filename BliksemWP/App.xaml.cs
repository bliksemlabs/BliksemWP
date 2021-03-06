﻿using System;
using System.Diagnostics;
using System.Resources;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Navigation;
using BliksemWP.Helpers;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BliksemWP.Resources;
using Windows.Storage;
using System.IO;
using System.IO.IsolatedStorage;
using SQLiteWinRT;
using System.Threading.Tasks;
using System.Threading;

namespace BliksemWP
{
    public partial class App : Application
    {
        /// <summary>
        /// The filename for database and stop databases. These are region specific.
        /// </summary>
        public static string STOPS_DB_NAME = "stops.db";
        public static string DATA_FILE_NAME = "timetable.dat";

        public static string INDEX_FILE_NAME = "index.xml";
        public static string INDEX_FILE_PATH = Path.Combine(ApplicationData.Current.LocalFolder.Path, INDEX_FILE_NAME);

        public static string KEY_REGION = "currentRegion";
        public static string KEY_REGION_LONG = "currentRegionLong";

        public static string BUNDLED_REGION_SHORT = "nl";
        public static string BUNDLED_REGION_LONG = "The Netherlands";
        public static DateTime BUNDLED_REGION_VALIDITY_START = new DateTime(2014, 05, 10);


        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public static PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions.
            UnhandledException += Application_UnhandledException;

            // Standard XAML initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Language display initialization
            InitializeLanguage();

        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private async void Application_Launching(object sender, LaunchingEventArgs e)
        {
            await SetupDataFiles();
        }

        private async Task SetupDataFiles()
        {
            Boolean copiedStops = !IsolatedStorageFile.GetUserStoreForApplication().FileExists(GetCurrentDataFilePath(STOPS_DB_NAME));

            copyResourceFile(GetCurrentDataFilePath(null), STOPS_DB_NAME).ContinueWith(t => PrepareFTSDatabase(copiedStops));
            await copyResourceFile(GetCurrentDataFilePath(null), DATA_FILE_NAME);
        }

        private async Task PrepareFTSDatabase(bool copiedStops)
        {
            // Only do this if we (re)loaded the database 
            if (copiedStops)
            {
                await PrepareFTSDatabase();
            }
        }

        public static string GetCurrentDataFilePath(string filename)
        {
            return GetCurrentDataFilePath(filename, false);
        }

        public static string GetCurrentDataFilePath(string filename, Boolean relative)
        {
            return GetCurrentDataFilePath(filename, GetRegion(), relative);
        }

        public static string GetCurrentDataFilePath(string filename, string region, Boolean relative) {
            if (filename == null)
            {
                filename = "";
            }
            string[] pieces;
            if (relative)
            {
                pieces = new string[] {  GetRegion(), filename};
            }
            else
            {
                pieces = new string[] { ApplicationData.Current.LocalFolder.Path, region, filename };
            }
            return Path.Combine(pieces);

        }


        public static string GetRegion()
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            if (!settings.Contains(KEY_REGION))
            {
                settings.Add(KEY_REGION, App.BUNDLED_REGION_SHORT);
                settings.Add(KEY_REGION_LONG, App.BUNDLED_REGION_LONG);
                settings.Save();
            }
            return (string)settings[KEY_REGION];
        }

        private static async System.Threading.Tasks.Task copyResourceFile(String pathName, String fileName)
        {
            StorageFile dbFile = null;
            try
            {
                // Try to get the 
                dbFile = await StorageFile.GetFileFromPathAsync(Path.Combine(pathName, fileName));
            }
            catch (FileNotFoundException)
            {
                if (dbFile == null)
                {
                    // Copy file from installation folder to local folder.
                    // Obtain the virtual store for the application.
                    IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication();
                    iso.CreateDirectory(pathName);

                    // Create a stream for the file in the installation folder.
                    using (Stream input = Application.GetResourceStream(new Uri(fileName, UriKind.Relative)).Stream)
                    {
                        // Create a stream for the new file in the local folder.
                        using (IsolatedStorageFileStream output = iso.CreateFile(Path.Combine(pathName, fileName)))
                        {
                            // Initialize the buffer.
                            byte[] readBuffer = new byte[4096];
                            int bytesRead = -1;

                            // Copy the file from the installation folder to the local folder. 
                            while ((bytesRead = input.Read(readBuffer, 0, readBuffer.Length)) > 0)
                            {
                                output.Write(readBuffer, 0, bytesRead);
                            }

                        }
                    }
                }
            }
        }

        private async Task PrepareFTSDatabase()
        {
            // Get the file from the install location  
            var file = await ApplicationData.Current.LocalFolder.GetFileAsync(GetCurrentDataFilePath(STOPS_DB_NAME));

            // Create a new SQLite instance for the file 
            var db = new Database(file);

            // Open the database asynchronously
            await db.OpenAsync(SqliteOpenMode.OpenReadWrite);

            // Execute a SQL statement
            await db.ExecuteStatementAsync("CREATE VIRTUAL TABLE stops USING fts4(stopindex, stopname);");

            db.Dispose();

        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Handle reset requests for clearing the backstack
            RootFrame.Navigated += CheckForResetNavigation;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        private void CheckForResetNavigation(object sender, NavigationEventArgs e)
        {
            // If the app has received a 'reset' navigation, then we need to check
            // on the next navigation to see if the page stack should be reset
            if (e.NavigationMode == NavigationMode.Reset)
                RootFrame.Navigated += ClearBackStackAfterReset;
        }

        private void ClearBackStackAfterReset(object sender, NavigationEventArgs e)
        {
            // Unregister the event so it doesn't get called again
            RootFrame.Navigated -= ClearBackStackAfterReset;

            // Only clear the stack for 'new' (forward) and 'refresh' navigations
            if (e.NavigationMode != NavigationMode.New && e.NavigationMode != NavigationMode.Refresh)
                return;

            // For UI consistency, clear the entire page stack
            while (RootFrame.RemoveBackEntry() != null)
            {
                ; // do nothing
            }
        }

        #endregion

        // Initialize the app's font and flow direction as defined in its localized resource strings.
        //
        // To ensure that the font of your application is aligned with its supported languages and that the
        // FlowDirection for each of those languages follows its traditional direction, ResourceLanguage
        // and ResourceFlowDirection should be initialized in each resx file to match these values with that
        // file's culture. For example:
        //
        // AppResources.es-ES.resx
        //    ResourceLanguage's value should be "es-ES"
        //    ResourceFlowDirection's value should be "LeftToRight"
        //
        // AppResources.ar-SA.resx
        //     ResourceLanguage's value should be "ar-SA"
        //     ResourceFlowDirection's value should be "RightToLeft"
        //
        // For more info on localizing Windows Phone apps see http://go.microsoft.com/fwlink/?LinkId=262072.
        //
        private void InitializeLanguage()
        {
            try
            {
                // Set the font to match the display language defined by the
                // ResourceLanguage resource string for each supported language.
                //
                // Fall back to the font of the neutral language if the Display
                // language of the phone is not supported.
                //
                // If a compiler error is hit then ResourceLanguage is missing from
                // the resource file.
                RootFrame.Language = XmlLanguage.GetLanguage(AppResources.General_ResourceLanguage);

                // Set the FlowDirection of all elements under the root frame based
                // on the ResourceFlowDirection resource string for each
                // supported language.
                //
                // If a compiler error is hit then ResourceFlowDirection is missing from
                // the resource file.
                FlowDirection flow = (FlowDirection)Enum.Parse(typeof(FlowDirection), AppResources.General_ResourceFlowDirection);
                RootFrame.FlowDirection = flow;

                //set language
                CultureHelper.SetCulture((Enums.Language)new SettingsHelper().GetValueOrDefault(SettingsHelper.Language, (int)Enums.Language.English));
            }
            catch
            {
                // If an exception is caught here it is most likely due to either
                // ResourceLangauge not being correctly set to a supported language
                // code or ResourceFlowDirection is set to a value other than LeftToRight
                // or RightToLeft.

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                throw;
            }
        }
    }
}