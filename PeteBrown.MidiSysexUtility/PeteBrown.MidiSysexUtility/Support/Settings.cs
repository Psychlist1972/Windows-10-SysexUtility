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

        private const string TipMidiOutputPortShown_key = "TipMidiOutputPortShown";
        private const string TipPickInputFileShown_key = "TipPickInputFileShown";
        private const string TipSendSysExFileShown_key = "TipSendSysExFileShown";


        public static uint TransferDelayBetweenBuffers
        {
            get { return GetValueOrDefault<uint>(TransferDelayBetweenBuffers_key, MidiSysExSender.DefaultDelayBetweenBuffers); }
            set { WriteValue<uint>(TransferDelayBetweenBuffers_key, value); }
        }

        public static bool TipMidiOutputPortShown
        {
            get { return GetValueOrDefault<bool>(TipMidiOutputPortShown_key, false); }
            set { WriteValue<bool>(TipMidiOutputPortShown_key, value); }
        }

        public static bool TipPickInputFileShown
        {
            get { return GetValueOrDefault<bool>(TipPickInputFileShown_key, false); }
            set { WriteValue<bool>(TipPickInputFileShown_key, value); }
        }

        public static bool TipSendSysExFileShown
        {
            get { return GetValueOrDefault<bool>(TipSendSysExFileShown_key, false); }
            set { WriteValue<bool>(TipSendSysExFileShown_key, value); }
        }
    }
}
