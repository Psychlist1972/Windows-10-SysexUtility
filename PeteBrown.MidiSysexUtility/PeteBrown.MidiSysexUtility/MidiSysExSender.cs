using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Midi;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace PeteBrown.MidiSysexUtility
{
    class MidiSysExSender
    {
        public const uint DefaultBufferSize = 128;
        public const uint DefaultDelayBetweenBuffers = 0;

        public static IAsyncOperationWithProgress<int, uint> SendSysExStreamAsyncWithProgress(IRandomAccessStream inputStream, IMidiOutPort outputPort, uint bufferSize = 128, uint sendDelayMilliseconds=0)
        {
            // TODO: if the first byte is not F0, this is not a binary SysEx file. Should check for that.

            var buffer = new Windows.Storage.Streams.Buffer(bufferSize);


            return AsyncInfo.Run<int, uint>((token, progress) =>
                Task.Run(async () =>
                {
                    using (var dataReader = new DataReader(inputStream))
                    {
                        uint bytesRead = 0;

                        await dataReader.LoadAsync((uint)inputStream.Size);

                        while (dataReader.UnconsumedBufferLength > 0)
                        {
                            // if the user has canceled, make sure we stop
                            if (token.IsCancellationRequested)
                            {
                                token.ThrowIfCancellationRequested();
                                return 0;
                            }
                            else
                            {
                                var bufferRead = dataReader.ReadBuffer(Math.Min(bufferSize, dataReader.UnconsumedBufferLength));

                                outputPort.SendBuffer(bufferRead);

                                bytesRead += bufferRead.Length;

                                if (sendDelayMilliseconds > 0)
                                {
                                    await Task.Delay((int)sendDelayMilliseconds);
                                }

                                progress.Report(bytesRead);
                            }
                        }


                        return 0;
                    }
                }));



        }
    }
}
