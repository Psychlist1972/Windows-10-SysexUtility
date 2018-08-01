using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeteBrown.MidiSysexUtility
{
    class Statistics : SettingsBase
    {
        private const string TotalFilesTransferred_key = "TotalFilesTransferred";
        private const string TotalBytesTransferred_key = "TotalBytesTransferred";
        private const string TotalCancelCount_key = "TotalCancelCount";
        private const string TotalErrorCount_key = "TotalErrorCount";

        // geek stats for the user. Will surface through UI
        public static uint TotalFilesTransferred
        {
            get { return GetValueOrDefault<uint>(TotalFilesTransferred_key, 0); }
            set { WriteValue<uint>(TotalFilesTransferred_key, value); }
        }

        // geek stats for the user. Will surface through UI
        public static ulong TotalBytesTransferred
        {
            get { return GetValueOrDefault<ulong>(TotalBytesTransferred_key, 0); }
            set { WriteValue<ulong>(TotalBytesTransferred_key, value); }
        }

        // geek stats for the user. Will surface through UI
        public static uint TotalCancelCount
        {
            get { return GetValueOrDefault<uint>(TotalCancelCount_key, 0); }
            set { WriteValue<uint>(TotalCancelCount_key, value); }
        }

        // geek stats for the user. Will surface through UI
        public static uint TotalErrorCount
        {
            get { return GetValueOrDefault<uint>(TotalErrorCount_key, 0); }
            set { WriteValue<uint>(TotalErrorCount_key, value); }
        }

    }
}
