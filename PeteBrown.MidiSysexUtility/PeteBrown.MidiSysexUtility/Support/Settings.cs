using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PeteBrown.MidiSysexUtility
{

    class Settings : SettingsBase
    {
        private const string TransferDelayBetweenBuffers_key = "TransferDelayBetweenBuffers";
        private const string TransferBufferSize_key = "TransferBufferSize";
        private const string F0Delay_key = "F0Delay";


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

        public static uint F0Delay
        {
            get { return GetValueOrDefault<uint>(F0Delay_key, MidiSysExSender.DefaultF0Delay); }
            set { WriteValue<uint>(F0Delay_key, value); }
        }



    }
}
