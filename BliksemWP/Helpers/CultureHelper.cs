using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using BliksemWP.Enums;

namespace BliksemWP.Helpers
{
    public static class CultureHelper
    {
        public static void SetCulture(Language language)
        {
            CultureInfo ci;

            switch (language)
            {
                case Language.English:
                    ci = new CultureInfo("en-US");
                    break;
                case Language.Nederlands:
                    ci = new CultureInfo("nl-NL");
                    break;
                case Language.Deutsch:
                    ci = new CultureInfo("de-DE");
                    break;
                case Language.Francais:
                    ci = new CultureInfo("fr-FR");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (Thread.CurrentThread.CurrentUICulture.Name == ci.Name)
            {
                return;
            }

            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;

            //save setting
            new SettingsHelper().AddOrUpdateValue(SettingsHelper.Language, language);

            //update resources
            ((LocalizedStrings)Application.Current.Resources["LocalizedStrings"]).UpdateLanguage();
        }
    }
}