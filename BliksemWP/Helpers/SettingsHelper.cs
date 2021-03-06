﻿using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BliksemWP.Helpers
{

    //TODO: improve needed?
    public class SettingsHelper
    {
        // Our isolated storage settings
        private IsolatedStorageSettings _settings;

        // The isolated storage key names of our settings
        public const string Language = "Language";

        /// <summary>
        /// Constructor that gets the application settings.
        /// </summary>
        public SettingsHelper()
        {
            // Get the settings for this application.
            _settings = IsolatedStorageSettings.ApplicationSettings;
        }

        /// <summary>
        /// Update a setting value for our application. If the setting does not
        /// exist, then add the setting.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool AddOrUpdateValue(string Key, Object value)
        {
            bool valueChanged = false;

            // If the key exists
            if (_settings.Contains(Key))
            {
                // If the value has changed
                if (_settings[Key] != value)
                {
                    // Store the new value
                    _settings[Key] = value;
                    valueChanged = true;
                    _settings.Save();
                }
            }
            // Otherwise create the key.
            else
            {
                _settings.Add(Key, value);
                valueChanged = true;
                _settings.Save();
            }
            return valueChanged;
        }

        /// <summary>
        /// Get the current value of the setting, or if it is not found, set the 
        /// setting to the default setting.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T GetValueOrDefault<T>(string Key, T defaultValue)
        {
            T value;

            // If the key exists, retrieve the value.
            if (_settings.Contains(Key))
            {
                value = (T)_settings[Key];
            }
            // Otherwise, use the default value.
            else
            {
                value = defaultValue;
            }
            return value;
        }
    }
}