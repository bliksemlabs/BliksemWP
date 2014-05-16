using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using BliksemWP.DataObjects;
using BliksemWP.Helpers;
using BliksemWP.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;

namespace BliksemWP
{
    public partial class InfoAndSettings : PhoneApplicationPage
    {
        public InfoAndSettings()
        {
            InitializeComponent();

            btnContact.Click += btnContact_Click;
            btnReview.Click += btnReview_Click;
            Loaded += InfoAndContactPage_Loaded;
        }

        void btnReview_Click(object sender, RoutedEventArgs e)
        {
            new MarketplaceReviewTask().Show();
        }

        void InfoAndContactPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadVersion();
            LoadLanguages();
        }

        private void LoadLanguages()
        {
            var source = new List<Language>();
            source.Add(new Language { Name = "English" });
            source.Add(new Language { Name = "Nederlands" });
            source.Add(new Language { Name = "Deutsch" });
            source.Add(new Language { Name = "Français" });
            lpLanguages.ItemsSource = source;

            int savedLanguageIndex = new SettingsHelper().GetValueOrDefault(SettingsHelper.Language, (int)Enums.Language.English);
            lpLanguages.SelectedIndex = savedLanguageIndex;
            lpLanguages.SelectionChanged += listPicker_SelectionChanged;
        }

        private void listPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //set culture of app
            CultureHelper.SetCulture((Enums.Language)(sender as ListPicker).SelectedIndex);
            LoadVersion();
        }

        private void LoadVersion()
        {
            string version = XDocument.Load("WMAppManifest.xml").Root.Element("App").Attribute("Version").Value;
            version = version.Substring(0, NthOccurence(version, '.', 3));
            txtVersion.Text = string.Format(AppResources.InfoAndSettingsPage_Info_Version, version);
        }

        void btnContact_Click(object sender, RoutedEventArgs e)
        {
            EmailComposeTask emailcomposer = new EmailComposeTask();
            emailcomposer.To = "info@bliksemlabs.nl"; //TODO: correct?
            emailcomposer.Subject = AppResources.InfoAndSettingsPage_Contact_MailSubject;
            emailcomposer.Body = "";
            emailcomposer.Show();
        }
        private int NthOccurence(string s, char t, int n)
        {
            return Regex.Matches(s, "[" + t + "]")[n - 1].Index;
        }
    }
}