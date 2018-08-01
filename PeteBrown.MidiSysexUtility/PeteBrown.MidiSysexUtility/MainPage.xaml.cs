using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PeteBrown.MidiSysexUtility
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private PeteBrown.Devices.Midi.MidiDeviceWatcher _watcher;

        private StorageFile _inputFile;
        private DeviceInformation _outputPortDeviceInformation;

        private IAsyncOperationWithProgress<int, MidiSysExSender.MidiSysExStatusReport> _transferOperation;
        ulong _fileSizeInBytes = 0;


        public MainPage()
        {
            this.InitializeComponent();

            _watcher = new Devices.Midi.MidiDeviceWatcher();


            ProgressPanel.Visibility = Visibility.Collapsed;

            Loaded += MainPage_Loaded;
        }




        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            EnteredBufferSize.Text = Settings.TransferBufferSize.ToString();
            EnteredTransferDelay.Text = Settings.TransferDelayBetweenBuffers.ToString();
            EnteredF0Delay.Text = Settings.F0Delay.ToString();


            _watcher.OutputPortsEnumerated += _watcher_OutputPortsEnumerated;
            _watcher.IgnoreBuiltInWavetableSynth = true;
            _watcher.EnumerateOutputPorts();
        }




        private async void _watcher_OutputPortsEnumerated(Devices.Midi.MidiDeviceWatcher sender)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    if (_watcher.OutputPortDescriptors.Count() > 0)
                    {
                        MidiOutputPortList.IsEnabled = true;

                        MidiOutputPortList.ItemsSource = _watcher.OutputPortDescriptors;
                    }
                    else
                    {
                        var dlg = new MessageDialog("No MIDI output ports found. Please connect your MIDI device.");
                        await dlg.ShowAsync();
                    }
                });
      

        }




        private async void PickInputFile_Click(object sender, RoutedEventArgs e)
        {
            _inputFile = null;
            InputFileName.Text = "";

            var picker = new FileOpenPicker();

            // I miss being able to add a description here, like "MIDI SysEx Files|*.syx;*.mid"
            picker.FileTypeFilter.Add(".syx");
            picker.FileTypeFilter.Add(".mid");
            picker.FileTypeFilter.Add("*");

            var file = await picker.PickSingleFileAsync();

            if (file != null)
            {

                _inputFile = file;

                SendSysExFile.IsEnabled = true;
                
                var basicProperties = await file.GetBasicPropertiesAsync();

                // update size info
                _fileSizeInBytes = basicProperties.Size;

                TotalBytes.Text = string.Format("{0:N0} bytes",_fileSizeInBytes);

                SysExSendProgressBar.Minimum = 0;
                SysExSendProgressBar.Maximum = (double)_fileSizeInBytes;

                PercentComplete.Text = "0%";

                InputFileName.Text = file.Name;

                ProgressPanel.Visibility = Visibility.Collapsed;

                TransferOperationInProgress.Text = "Initializing...";

            }
            else
            {
                SendSysExFile.IsEnabled = false;
            }

        }




        private async void SendSysExFile_Click(object sender, RoutedEventArgs e)
        {
            // validate the two user-entered parameters

            uint transferBufferSize = 0;
            if ((!uint.TryParse(EnteredBufferSize.Text, out transferBufferSize)) || transferBufferSize <= 0)
            {
                var dlg = new MessageDialog("For the buffer size, please enter a whole number > 0. This is the size of the message sent over MIDI.");
                await dlg.ShowAsync();
                return;
            }

            uint transferDelayBetweenBuffers = 0;
            if ((!uint.TryParse(EnteredTransferDelay.Text, out transferDelayBetweenBuffers)) || transferDelayBetweenBuffers < 0)
            {
                var dlg = new MessageDialog("For the send delay, please enter a positive whole number. This is the delay between buffers sent over MIDI.");
                await dlg.ShowAsync();
                return;
            }

            uint f0Delay = 0;
            if ((!uint.TryParse(EnteredF0Delay.Text, out f0Delay)) || f0Delay < 0)
            {
                var dlg = new MessageDialog("For the F0 delay, please enter a positive whole number. This is the delay after the initial byte is sent to the device, starting the SysEx conversation.");
                await dlg.ShowAsync();
                return;
            }



            // validate, open the port, and then send the file
            if (_inputFile != null)
            {
                if (_outputPortDeviceInformation != null)
                {
                    // open the MIDI port
                    var outputPort = await MidiOutPort.FromIdAsync(_outputPortDeviceInformation.Id);

                    if (outputPort != null)
                    {
                        // open the file
                        var stream = await _inputFile.OpenReadAsync();

                        if (stream != null && stream.CanRead)
                        {
                            // send the bytes
                            _transferOperation = MidiSysExSender.SendSysExStreamAsyncWithProgress(stream, outputPort, transferBufferSize, transferDelayBetweenBuffers);

                            // show progress of the operation. This is updated async
                            ProgressPanel.Visibility = Visibility.Visible;
                            Cancel.IsEnabled = true;


                            //report progress 
                            _transferOperation.Progress = (result, progress) => 
                            {
                                SysExSendProgressBar.Value = (double)progress.BytesRead;
                                ProgressBytes.Text = string.Format("{0:N0}", progress.BytesRead);
                                PercentComplete.Text = Math.Round(((double)progress.BytesRead / _fileSizeInBytes) * 100, 0) + "%";

                                TransferOperationInProgress.Text = MidiSysExSender.GetStageDescription(progress.Stage);
                            };

                            // handle completion
                            _transferOperation.Completed = async (result, progress) =>
                            {
                                SysExSendProgressBar.Value = (double)_fileSizeInBytes;
                                ProgressBytes.Text = string.Format("{0:N0}", _fileSizeInBytes);
                                PercentComplete.Text = "100%";

                                // no need for cancel anymore
                                Cancel.IsEnabled = false;

                                // nuke the stream
                                stream.Dispose();
                                stream = null;

                                // close the MIDI port
                                outputPort = null;

                                // show completion message, depending on what type of completion we have
                                if (result.Status == AsyncStatus.Canceled)
                                {
                                    Statistics.TotalCancelCount += 1;

                                    var dlg = new MessageDialog("Transfer canceled.");
                                    await dlg.ShowAsync();
                                }
                                else if (result.Status == AsyncStatus.Error)
                                {
                                    Statistics.TotalErrorCount += 1;

                                    var dlg = new MessageDialog("Transfer error. You may need to close and re-open this app, and likely also reboot your device.");
                                    await dlg.ShowAsync();
                                }
                                else
                                {
                                    // save the user-entered settings, since they worked
                                    Settings.TransferBufferSize = transferBufferSize;
                                    Settings.TransferDelayBetweenBuffers = transferDelayBetweenBuffers;
                                    Settings.F0Delay = f0Delay;

                                    // update user stats (for local use and display)
                                    Statistics.TotalFilesTransferred += 1;
                                    Statistics.TotalBytesTransferred += _fileSizeInBytes;

                                    NotificationManager.NotifySuccess(_fileSizeInBytes, _inputFile.Name);
                                }

                            };

                        }
                        else
                        {
                            // stream is null or CanRead is false
                            var dlg = new MessageDialog("Could not open file '" + _inputFile.Name + "' for reading.");
                            await dlg.ShowAsync();
                        }
                    }
                    else
                    {
                        // outputPort is null
                        var dlg = new MessageDialog("Could not open MIDI output port '" + _outputPortDeviceInformation.Name + "'");
                        await dlg.ShowAsync();
                    }
                }
                else
                {
                    // _outputPortDeviceInformation is null
                    var dlg = new MessageDialog("No MIDI output port selected'");
                    await dlg.ShowAsync();
                }
            }
            else
            {
                // _inputFile is null
                var dlg = new MessageDialog("No SysEx input file selected'");
                await dlg.ShowAsync();
            }
        }



        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Stop any in-process transfer
            _transferOperation.Cancel();

            Cancel.IsEnabled = false;
        }



        private void MidiOutputPortList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // enable the file button if the user has selected a port
            if (e.AddedItems.Count > 0 && MidiOutputPortList.SelectedItem != null)
            {
                PickInputFile.IsEnabled = true;

                _outputPortDeviceInformation = (DeviceInformation)MidiOutputPortList.SelectedItem;

                ProgressPanel.Visibility = Visibility.Collapsed;
            }
        }






    }
}
