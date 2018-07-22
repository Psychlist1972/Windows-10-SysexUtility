using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PeteBrown.MidiSysexUtility
{
    class Settings
    {
        private const string TransferDelayBetweenBuffers_key = "TransferDelayBetweenBuffers";
        private const string TransferBufferSize_key = "TransferBufferSize";

        private static T GetValueOrDefault<T>(string key, T defaultValue)
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

        private static void WriteValue<T>(string key, T value)
        {
            ApplicationData.Current.RoamingSettings.Values[key] = value;
        }

        public static uint TransferDelayBetweenBuffers
        {
            get { return GetValueOrDefault<uint>(TransferDelayBetweenBuffers_key, MidiSysExSender.DefaultDelayBetweenBuffers); }
            set { WriteValue<uint>(TransferDelayBetweenBuffers_key, value); }
        }


        public static uint TransferBufferSize
        {
            get { return GetValueOrDefault<uint>(TransferBufferSize_key, MidiSysExSender.DefaultBufferSize); }
            set { WriteValue<uint>(TransferBufferSize_key, value); }
        }
    }
}
