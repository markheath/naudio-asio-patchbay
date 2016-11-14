using System;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.Asio;

namespace NAudioAsioPatchBay
{
    public class AsioInputPatcher : ISampleProvider
    {
        private readonly int outputChannels;
        private readonly int inputChannels;
        private readonly float[,] routingMatrix;
        private float[] mixBuffer;

        public AsioInputPatcher(int sampleRate, int inputChannels, int outputChannels)
        {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, outputChannels);
            this.outputChannels = outputChannels;
            this.inputChannels = inputChannels;
            routingMatrix = new float[inputChannels, outputChannels];
            // initial routing is each input straight to the same number output
            for (int n = 0; n < Math.Min(inputChannels, outputChannels); n++)
            {
                routingMatrix[n, n] = 1.0f;
            }
        }


        // here we get given all the input channels we recorded from
        public void ProcessBuffer(IntPtr[] inBuffers, IntPtr[] outBuffers, int sampleCount, AsioSampleType sampleType)
        {
            Func<IntPtr, int, float> getInputSample;
            if (sampleType == AsioSampleType.Int32LSB)
                getInputSample = GetInputSampleInt32LSB;
            else if (sampleType == AsioSampleType.Int16LSB)
                getInputSample = GetInputSampleInt16LSB;
            else if (sampleType == AsioSampleType.Int24LSB)
                getInputSample = GetInputSampleInt24LSB;
            else if (sampleType == AsioSampleType.Float32LSB)
                getInputSample = GetInputSampleFloat32LSB;
            else
                throw new ArgumentException($"Unsupported ASIO sample type {sampleType}");

            int offset = 0;
            mixBuffer = BufferHelpers.Ensure(mixBuffer, sampleCount * outputChannels);
            
            for (int n = 0; n < sampleCount; n++)
            {
                for (int outputChannel = 0; outputChannel < outputChannels; outputChannel++)
                {
                    mixBuffer[offset] = 0.0f;
                    for (int inputChannel = 0; inputChannel < inputChannels; inputChannel++)
                    {
                        // mix in the desired amount
                        var amount = routingMatrix[inputChannel, outputChannel];
                        if (amount > 0)
                            mixBuffer[offset] += amount * getInputSample(inBuffers[inputChannel], n);
                    }
                    offset++;
                }
            }

            Action<IntPtr, int, float> setOutputSample;
            if (sampleType == AsioSampleType.Int32LSB)
                setOutputSample = SetOutputSampleInt32LSB;
            else if (sampleType == AsioSampleType.Int16LSB)
                setOutputSample = SetOutputSampleInt16LSB;
            else if (sampleType == AsioSampleType.Int24LSB)
                throw new InvalidOperationException("Not supported");
            else if (sampleType == AsioSampleType.Float32LSB)
                setOutputSample = SetOutputSampleFloat32LSB;
            else
                throw new ArgumentException($"Unsupported ASIO sample type {sampleType}");


            // now write to the output buffers
            offset = 0;
            for (int n = 0; n < sampleCount; n++)
            {
                for (int outputChannel = 0; outputChannel < outputChannels; outputChannel++)
                {
                    setOutputSample(outBuffers[outputChannel], n, mixBuffer[offset++]);
                }
            }
        }

        private unsafe void SetOutputSampleInt32LSB(IntPtr buffer, int n, float value)
        {
            *((int*)buffer + n) = (int)(value * int.MaxValue);
        }

        private unsafe float GetInputSampleInt32LSB(IntPtr inputBuffer, int n)
        {
            return *((int*)inputBuffer + n) / (float)int.MaxValue;
        }

        private unsafe float GetInputSampleInt16LSB(IntPtr inputBuffer, int n)
        {
            return *((short*)inputBuffer + n) / (float)short.MaxValue;
        }

        private unsafe void SetOutputSampleInt16LSB(IntPtr buffer, int n, float value)
        {
            *((short*)buffer + n) = (short)(value * short.MaxValue);
        }

        private unsafe float GetInputSampleInt24LSB(IntPtr inputBuffer, int n)
        {
            byte* pSample = (byte*)inputBuffer + n * 3;
            int sample = pSample[0] | (pSample[1] << 8) | ((sbyte)pSample[2] << 16);
            return sample / 8388608.0f;
        }


        private unsafe float GetInputSampleFloat32LSB(IntPtr inputBuffer, int n)
        {
            return *((float*) inputBuffer + n);
        }

        private unsafe void SetOutputSampleFloat32LSB(IntPtr buffer, int n, float value)
        {
            *((float*) buffer + n) = value;
        }

        public float[,] RoutingMatrix => routingMatrix;

        // immediately after SetInputSamples, we are now asked for all the audio we want
        // to write to the soundcard
        public int Read(float[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Should not be called");
        }

        public WaveFormat WaveFormat { get; }
    }
}