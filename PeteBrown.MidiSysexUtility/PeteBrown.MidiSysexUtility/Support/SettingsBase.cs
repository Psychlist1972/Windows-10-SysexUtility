using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PeteBrown.MidiSysexUtility
{
    abstract class SettingsBase
    {

        protected static T GetValueOrDefault<T>(string key, T defaultValue)
        {
            try
            {
                if (ApplicationData.Current.RoamingSettings.Values.Keys.Contains(key))
                {
                    return (T)ApplicationData.Current.RoamingSettings.Values[key];
                }
                else
                {
                    WriteValue(key, defaultValue);

                    return defaultValue;
                }
            }
            catch
            {
                Analytics.LogEvent(AnalyticsEvent.SettingsReadError);

                return defaultValue;
            }
        }

        protected static void WriteValue<T>(string key, T value)
        {
            try
            {
                ApplicationData.Current.RoamingSettings.Values[key] = value;
            }
            catch
            {
                Analytics.LogEvent(AnalyticsEvent.SettingsWriteError);
            }
        }

    }
}
