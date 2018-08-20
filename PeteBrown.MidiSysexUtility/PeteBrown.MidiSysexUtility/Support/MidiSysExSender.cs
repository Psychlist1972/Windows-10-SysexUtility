using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Midi;
using Windows.Foundation;
using Windows.Storage.Streams;
using System.Reflection;

namespace PeteBrown.MidiSysexUtility
{

    public class MidiSysExSender
    {
        public enum MidiSysExStatusStages
        {
            [Description("Sending initial F0 byte")]
            SendingF0,

            [Description("Sent F0 byte")]
            SentF0,

            [Description("Sending data")]
            SendingData,

            [Description("Transfer complete")]
            Complete
        }

        public static string GetStageDescription(MidiSysExSender.MidiSysExStatusStages stage)
        {
            Type type = stage.GetType();
            string name = Enum.GetName(type, stage);

            if (name != null)
            {
                FieldInfo field = type.GetField(name);

                if (field != null)
                {
                    DescriptionAttribute attr =
                           Attribute.GetCustomAttribute(field,
                             typeof(DescriptionAttribute)) as DescriptionAttribute;

                    if (attr != null)
                    {
                        return attr.Description;
                    }
                }
            }

            return stage.ToString();
        }

        public struct MidiSysExStatusReport
        {
            public MidiSysExStatusStages Stage { get; internal set; }
            public uint BytesRead { get; internal set; }

        }



        public const uint DefaultBufferSize = 256;
        public const uint DefaultDelayBetweenBuffers = 0;
        public const uint DefaultF0Delay = 0;

        public static IAsyncOperationWithProgress<int, MidiSysExStatusReport> SendSysExStreamAsyncWithProgress(
            IRandomAccessStream inputStream, 
            IMidiOutPort outputPort, 
            uint bufferSize = DefaultBufferSize, 
            uint sendDelayMilliseconds= DefaultDelayBetweenBuffers, 
            uint f0DelayMilliseconds= DefaultF0Delay)
        {
            // TODO: if the first byte is not F0, this is not a binary SysEx file. Should check for that.

            var buffer = new Windows.Storage.Streams.Buffer(bufferSize);


            return AsyncInfo.Run<int, MidiSysExStatusReport>((token, progress) =>
                Task.Run(async () =>
                {
                    using (var dataReader = new DataReader(inputStream))
                    {
                        uint bytesRead = 0;

                        await dataReader.LoadAsync((uint)inputStream.Size);

                        progress.Report(new MidiSysExStatusReport() { Stage = MidiSysExStatusStages.SendingF0, BytesRead = 0 });

                        if (f0DelayMilliseconds > 0 && dataReader.UnconsumedBufferLength > 0)
                        {
                            // read the first byte, and then pause

                            var f0Buffer = dataReader.ReadBuffer(1);
                            outputPort.SendBuffer(f0Buffer);

                            bytesRead += 1;

                            await Task.Delay((int)f0DelayMilliseconds);

                            progress.Report(new MidiSysExStatusReport() { Stage = MidiSysExStatusStages.SentF0, BytesRead = bytesRead});
                        }

                        while (dataReader.UnconsumedBufferLength > 0)
                        {
                            // if the user canceled, throw, so we get ejected from this
                            token.ThrowIfCancellationRequested();

                            // otherwise continue
                            var bufferRead = dataReader.ReadBuffer(Math.Min(bufferSize, dataReader.UnconsumedBufferLength));

                            outputPort.SendBuffer(bufferRead);

                            bytesRead += bufferRead.Length;

                            if (sendDelayMilliseconds > 0)
                            {
                                await Task.Delay((int)sendDelayMilliseconds);
                            }
                            
                            progress.Report(new MidiSysExStatusReport() { Stage = MidiSysExStatusStages.SendingData, BytesRead = bytesRead });
                        }


                        progress.Report(new MidiSysExStatusReport() { Stage = MidiSysExStatusStages.Complete, BytesRead = bytesRead });

                        return 0;
                    }
                }));



        }
    }
}
