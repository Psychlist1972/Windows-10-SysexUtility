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

        protected static void WriteValue<T>(string key, T value)
        {
            ApplicationData.Current.RoamingSettings.Values[key] = value;
        }

    }
}
