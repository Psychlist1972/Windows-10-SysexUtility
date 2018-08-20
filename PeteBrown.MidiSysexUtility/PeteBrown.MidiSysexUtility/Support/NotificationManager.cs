using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Services.Store;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;

namespace PeteBrown.MidiSysexUtility
{
    class NotificationManager
    {

        private static bool _alreadyPromptedForRatingThisSession = false;



        private static ToastContent GenerateSuccessToastContent(ulong bytesTransferred, string fileName)
        {
            return new ToastContent()
            {
                Scenario = ToastScenario.Default,

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = "SysEx File Transfer Complete"
                            },
                            new AdaptiveText()
                            {
                                Text = fileName
                            },
                            new AdaptiveText()
                            {
                                Text = string.Format("{0:N0} bytes transferred", bytesTransferred)
                            }
                        }
                    }
                }


            };
        }


        private static ToastContent GenerateErrorToastContent(ulong bytesTransferred, string fileName)
        {
            return new ToastContent()
            {
                Scenario = ToastScenario.Default,

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = "SysEx File Transfer Error"
                            },
                            new AdaptiveText()
                            {
                                Text = fileName
                            },
                            new AdaptiveText()
                            {
                                Text = string.Format("{0:N0} bytes transferred", bytesTransferred)
                            }
                        }
                    }
                }


            };
        }


        public static async void NotifySuccess(ulong bytesTransferred, string fileName)
        {
            try
            {
                // check to see if app has focus. If so, pop a dialog. If not, pop a toast
                if (GlobalNonPersistentState.IsCurrentlyActiveApp)
                {
                    var dlg = new MessageDialog(string.Format("Transfer complete. Bytes transferred {0:N0}", bytesTransferred), "Transfer Complete");
                    await dlg.ShowAsync();


                    // App is in foreground, and the transfer was successful. Let's ask the user for a rating,
                    // but only if they haven't previously rated the app, and we haven't already asked them during
                    // this app usage session
                    if (!Statistics.UserHasRatedApp && !_alreadyPromptedForRatingThisSession)
                    {
                        ShowRatingReviewDialog();
                    }

                }
                else
                {
                    ToastContent toastContent = GenerateSuccessToastContent(bytesTransferred, fileName);
                    ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(toastContent.GetXml()));
                }
            }
            catch
            {
                // don't bomb out on a notification problem
                Analytics.LogEvent(AnalyticsEvent.NotificationSuccessError);
            }
        }

        // code based on this: https://docs.microsoft.com/en-us/windows/uwp/monetize/request-ratings-and-reviews
        private async static void ShowRatingReviewDialog()
        {
            try
            {
                _alreadyPromptedForRatingThisSession = true;

                StoreSendRequestResult result = await StoreRequestHelper.SendRequestAsync(
                    StoreContext.GetDefault(), 16, String.Empty);

                if (result.ExtendedError == null)
                {
                    JObject jsonObject = JObject.Parse(result.Response);
                    if (jsonObject.SelectToken("status").ToString() == "success")
                    {
                        // The customer rated or reviewed the app.
                        Statistics.UserHasRatedApp = true;

                    }
                    else if (jsonObject.SelectToken("status").ToString() == "aborted")
                    {
                        // The customer chose not to rate the app
                        Statistics.UserHasRatedApp = false;
                    }
                }
                else
                {
                    // There was an error rating the app

                }
            }
            catch
            {
                // don't crash app due to failure with rating / review
                Analytics.LogEvent(AnalyticsEvent.RatingReviewError);
            }
        }




        public static async void NotifyError(ulong bytesTransferred, string fileName)
        {
            try
            {
                // check to see if app has focus. If so, pop a dialog. If not, pop a toast
                if (GlobalNonPersistentState.IsCurrentlyActiveApp)
                {
                    var dlg = new MessageDialog(string.Format("Transfer Error. Bytes transferred {0:N0}", bytesTransferred), "Transfer Error");
                    await dlg.ShowAsync();
                }
                else
                {
                    ToastContent toastContent = GenerateSuccessToastContent(bytesTransferred, fileName);
                    ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(toastContent.GetXml()));
                }
            }
            catch
            {
                Analytics.LogEvent(AnalyticsEvent.NotificationErrorError);
            }
        }


    }
}
