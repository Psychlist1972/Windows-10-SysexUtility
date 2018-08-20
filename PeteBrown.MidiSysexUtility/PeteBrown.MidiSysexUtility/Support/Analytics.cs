using Microsoft.Services.Store.Engagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PeteBrown.MidiSysexUtility
{
    public enum AnalyticsEvent
    {
        TransferError,
        TransferSuccess,
        TransferCancel,

        NotificationErrorError,
        NotificationSuccessError,
        RatingReviewError,

        SettingsReadError,
        SettingsWriteError,

        TransferCancelError
    }

    class Analytics
    {
        private static StoreServicesCustomEventLogger _logger = null;

        public static void LogEvent(AnalyticsEvent eventToLog)
        {
            try
            {
                VerifyLogger();

                _logger.Log(eventToLog.ToString());
            }
            catch
            {
                // don't do anything, but don't want to crash app due to failure in logging event
            }
        }

        private static void VerifyLogger()
        {
            if (_logger == null)
            {
                _logger = StoreServicesCustomEventLogger.GetDefault();
            }
        }

    }
}
