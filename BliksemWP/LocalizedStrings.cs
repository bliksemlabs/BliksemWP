using System;
using System.ComponentModel;
using BliksemWP.Resources;

namespace BliksemWP
{
    /// <summary>
    /// Provides access to string resources.
    /// </summary>
    public class LocalizedStrings : INotifyPropertyChanged
    {
        private static AppResources _localizedResources = new AppResources();

        public AppResources LocalizedResources { get { return _localizedResources; } }

        public void UpdateLanguage()
        {
            _localizedResources = new AppResources();
            NotifyPropertyChanged("LocalizedResources");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}