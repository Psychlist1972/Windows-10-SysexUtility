using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;

namespace PeteBrown.MidiSysexUtility
{
    class NotificationManager
    {




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


        public static async void NotifySuccess(ulong bytesTransferred, string fileName)
        {

            // check to see if app has focus. If so, pop a dialog. If not, pop a toast
            if (GlobalNonPersistentState.IsCurrentlyActiveApp)
            {
                var dlg = new MessageDialog(string.Format("Transfer complete. Bytes transferred {0:N0}", bytesTransferred));
                await dlg.ShowAsync();
            }
            else
            {
                ToastContent toastContent = GenerateSuccessToastContent(bytesTransferred, fileName);
                ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(toastContent.GetXml()));
            }
        }



    }
}
