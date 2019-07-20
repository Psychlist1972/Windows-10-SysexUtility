using System;
using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Midi;
using Windows.Foundation;
using Windows.Storage.Streams;
using System.Reflection;

namespace PeteBrown.MidiSysexUtility
{

    public class MidiSysExSender
    {
        const byte SysexBeginMessageByte = (byte)0xf0;
        const byte SysexEndMessageByte = (byte)0xf7;


        public enum MidiSysExStatusStages
        {
            [Description("Measuring max message size")]
            MeasuringMessageSize,

            [Description("Validating file")]
            ValidatingFile,

            [Description("Sending data")]
            SendingData,

            [Description("Transfer complete")]
            Complete
        }

        public enum MidiSysExFileValidationStatus
        {
            [Description("OK")]
            OK,

            [Description("Invalid SysEx file. File does not begin with the required 0xF0 byte. Perhaps it is not a binary SysEx file? ASCII SysEx files are not currently supported.")]
            MissingF0Error,

            [Description("Invalid SysEx file. F0 and F7 pairs not matched up.")]
            F0F7MismatchError,

            [Description("Invalid SysEx file. File is not large enough to be a valid SysEx file")]
            FileTooSmallError,

            [Description("Invalid SysEx file. File does not end with the required 0xF7 byte. Perhaps it is not a binary SysEx file? ASCII SysEx files are not currently supported.")]
            MissingF7Error
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

            // if all else fails
            return stage.ToString();
        }





        public struct MidiSysExStatusReport
        {
            public MidiSysExStatusStages Stage { get; internal set; }

            public uint BytesRead { get; internal set; }

        }

        public struct MeasureAndValidateSysExFileResult
        {
            public MidiSysExFileValidationStatus Status { get; internal set; }

            public uint BufferSize { get; internal set; }

            public uint MessageCount { get; internal set; }

            public uint FileSize { get; internal set; }

            public string GetStatusDescription()
            {
                Type type = Status.GetType();
                string name = Enum.GetName(type, Status);

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

                // if all else fails
                return Status.ToString();
            }
        }

        public async static Task<MeasureAndValidateSysExFileResult> MeasureAndValidateSysExFile(IRandomAccessStream inputStream)
        {
            MeasureAndValidateSysExFileResult result = new MeasureAndValidateSysExFileResult();


            // I removed the using on this datareader as disposing it also disposed the stream
            // but now the datareader is out there, hanging.
            var dataReader = new DataReader(inputStream);

            uint bytesReadForThisMessage = 0;

            // initial file load
            await dataReader.LoadAsync((uint)inputStream.Size);

            // Measure the largest message so the user doesn't have to. 
            // We use this to size the message buffer, which for BLE MIDI, must contain 
            // no more or less than one complete sysex message (f0 to f7)

            byte b = 0x00;

            // technically, should be quite a bit larger, but not sure I want to bother trying to measure that 
            // with all the random implementations out there.
            if (dataReader.UnconsumedBufferLength < 2)
            {
                result.Status = MidiSysExFileValidationStatus.FileTooSmallError;

                return result;
            }

            bool firstByte = true;
            int pairing = 0;

            while (dataReader.UnconsumedBufferLength > 0)
            {
                // read next character
                b = dataReader.ReadByte();
                bytesReadForThisMessage += 1;
                result.FileSize += 1;

                if (firstByte && b != SysexBeginMessageByte)
                {
                    result.Status = MidiSysExFileValidationStatus.MissingF0Error;
                    return result;
                }


                if (b == SysexBeginMessageByte)
                {
                    pairing += 1;
                }
                else if (b == SysexEndMessageByte)
                {
                    result.MessageCount += 1;
                    result.BufferSize = Math.Max(bytesReadForThisMessage, result.BufferSize);

                    pairing -= 1;

                    bytesReadForThisMessage = 0;
                }

                firstByte = false;
            }

            // check last byte
            if (b != SysexEndMessageByte)
            {
                result.Status = MidiSysExFileValidationStatus.MissingF7Error;
                return result;
            }

            // check pairs
            if (pairing != 0)
            {
                result.Status = MidiSysExFileValidationStatus.F0F7MismatchError;
                return result;
            }

            result.Status = MidiSysExFileValidationStatus.OK;
            return result;
        }

        public const uint DefaultDelayBetweenBuffers = 50;

        public static IAsyncOperationWithProgress<int, MidiSysExStatusReport> SendSysExStreamAsyncWithProgress(
            IRandomAccessStream inputStream, 
            IMidiOutPort outputPort, 
            uint bufferSize,
            uint sendDelayMilliseconds= DefaultDelayBetweenBuffers)
        {

            return AsyncInfo.Run<int, MidiSysExStatusReport>((token, progress) =>
                Task.Run(async () =>
                {
                    using (var dataReader = new DataReader(inputStream))
                    {
                        uint bytesRead = 0;

                        // allocate our message buffer
                        var buffer = new byte[bufferSize];

                        System.Diagnostics.Debug.WriteLine("Using transfer buffer size " + bufferSize + " bytes.");

                        progress.Report(new MidiSysExStatusReport()
                        { Stage = MidiSysExStatusStages.SendingData, BytesRead = 0 });


                        // reset/reload the input stream
                        inputStream.Seek(0);
                        await dataReader.LoadAsync((uint)inputStream.Size);
                        bytesRead = 0;


                        // start actually sending data

                        uint buffIndex = 0;

                        while (dataReader.UnconsumedBufferLength > 0)
                        {
                            byte b = 0;
                            buffIndex = 0;

                            // This loop builds up one complete sysex message from f0 to f7
                            while (b != SysexEndMessageByte && dataReader.UnconsumedBufferLength > 0)
                            {
                                // if the user canceled, throw, so we get ejected from this
                                token.ThrowIfCancellationRequested();

                                // read next character
                                b = dataReader.ReadByte();

                                // append to buffer                                                              

                                buffer[buffIndex] = b;

                                buffIndex += 1;
                                bytesRead += 1;
                            }

                            progress.Report(new MidiSysExStatusReport()
                            { Stage = MidiSysExStatusStages.SendingData, BytesRead = bytesRead });

                            uint length = buffIndex;
                            var messageBuffer = new Windows.Storage.Streams.Buffer(length);
                            buffer.CopyTo(0, messageBuffer, 0, (int)length);
                            messageBuffer.Length = length;

                            //foreach (byte db in messageBuffer.ToArray())
                            //{
                            //    System.Diagnostics.Debug.Write(string.Format("{0:X2} ", db));
                            //}
                            //System.Diagnostics.Debug.WriteLine("");

                            outputPort.SendBuffer(messageBuffer);

                            if (sendDelayMilliseconds > 0)
                            {
                                await Task.Delay((int)sendDelayMilliseconds);
                            }
                        }

                        progress.Report(new MidiSysExStatusReport()
                        { Stage = MidiSysExStatusStages.Complete, BytesRead = bytesRead });

                        return 0;
                    }
                }));



        }
    }
}
