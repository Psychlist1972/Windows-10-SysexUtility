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

        private IAsyncOperationWithProgress<int, uint> _transferOperation;
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
            EnteredBufferSize.Text = MidiSysExSender.DefaultBufferSize.ToString();
            EnteredSendDelay.Text = MidiSysExSender.DefaultDelayBetweenBuffers.ToString();


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

            picker.FileTypeFilter.Add(".syx");
            picker.FileTypeFilter.Add(".mid");

            var file = await picker.PickSingleFileAsync();

            if (file != null)
            {

                _inputFile = file;

                SendSysExFile.IsEnabled = true;


                
                var basicProperties = await file.GetBasicPropertiesAsync();

                // update size info
                _fileSizeInBytes = basicProperties.Size;

                TotalBytes.Text = _fileSizeInBytes + " bytes";

                SysExSendProgressBar.Minimum = 0;
                SysExSendProgressBar.Maximum = (double)_fileSizeInBytes;

                PercentComplete.Text = "0%";

                InputFileName.Text = file.Name;

                ProgressPanel.Visibility = Visibility.Collapsed;

            }
            else
            {
                SendSysExFile.IsEnabled = false;
            }

        }




        private async void SendSysExFile_Click(object sender, RoutedEventArgs e)
        {
            // validate the two user-entered parameters

            int sendDelay = 0;
            int bufferSize = 0;

            if ((int.TryParse(EnteredSendDelay.Text, out sendDelay)) && sendDelay >= 0)
            {
                if ((int.TryParse(EnteredBufferSize.Text, out bufferSize)) && bufferSize > 0)
                {
                    // all good
                }
                else
                {
                    var dlg = new MessageDialog("For the buffer size, please enter a whole number > 0. This is the size of the message sent over MIDI.");
                    await dlg.ShowAsync();
                    return;
                }
            }
            else
            {
                var dlg = new MessageDialog("For the send delay, please enter a positive whole number. This is the delay between buffers sent over MIDI.");
                await dlg.ShowAsync();
                return;
            }

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
                            _transferOperation = MidiSysExSender.SendSysExStreamAsyncWithProgress(stream, outputPort, (uint)bufferSize, (uint)sendDelay);

                            ProgressPanel.Visibility = Visibility.Visible;

                            Cancel.IsEnabled = true;

                            //report progress 
                            _transferOperation.Progress = (result, progress) => 
                            {
                                SysExSendProgressBar.Value = (double)progress;
                                ProgressBytes.Text = progress.ToString();
                                PercentComplete.Text = Math.Round(((double)progress / _fileSizeInBytes) * 100, 0) + "%";
                            };

                            _transferOperation.Completed = async (result, progress) =>
                            {
                                // disable other UI


                                // close the stream
                                stream.Dispose();


                                // close the MIDI port
                                outputPort = null;

                                // re-enable other UI


                                // show completion message

                                Cancel.IsEnabled = false;

                                if (result.Status == AsyncStatus.Canceled)
                                {
                                    var dlg = new MessageDialog("Transfer canceled.");
                                    await dlg.ShowAsync();
                                }
                                else if (result.Status == AsyncStatus.Error)
                                {
                                    var dlg = new MessageDialog("Transfer error.");
                                    await dlg.ShowAsync();
                                }
                                else
                                {
                                    var dlg = new MessageDialog("Transfer complete.");
                                    await dlg.ShowAsync();
                                }

                            };

                        }
                        else
                        {
                            var dlg = new MessageDialog("Could not open file '" + _inputFile.Name + "' for reading.");
                            await dlg.ShowAsync();
                        }
                    }
                    else
                    {
                        var dlg = new MessageDialog("Could not open MIDI output port '" + _outputPortDeviceInformation.Name + "'");
                        await dlg.ShowAsync();
                    }
                }
                else
                {
                    var dlg = new MessageDialog("No MIDI output port selected'");
                    await dlg.ShowAsync();
                }
            }
            else
            {
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
            // enable the file button

            if (e.AddedItems.Count > 0 && MidiOutputPortList.SelectedItem != null)
            {
                PickInputFile.IsEnabled = true;

                _outputPortDeviceInformation = (DeviceInformation)MidiOutputPortList.SelectedItem;

                ProgressPanel.Visibility = Visibility.Collapsed;
            }
        }
    }
}
