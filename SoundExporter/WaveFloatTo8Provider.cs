﻿using System;
using NAudio.Wave;

namespace SoundExporter
{
    public class WaveFloatTo8Provider : IWaveProvider
    {
        const int SourceBitsPerSample = sizeof(float) * 8;
        const int TargetBitsPerSample = 8;

        readonly ISampleProvider _sourceProvider;
        readonly WaveFormat _waveFormat;

        float[] _sourceBuffer;

        public WaveFloatTo8Provider(ISampleProvider sourceProvider)
        {
            var sourceFormat = sourceProvider.WaveFormat;

            if (sourceFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new ArgumentException("Input source provider must be IEEE float", nameof(sourceProvider));
            }

            if (sourceFormat.BitsPerSample != SourceBitsPerSample)
            {
                throw new ArgumentException(
                    $"Input source provider must be {SourceBitsPerSample} bits", nameof(sourceProvider));
            }

            _sourceProvider = sourceProvider;
            _waveFormat = new WaveFormat(sourceFormat.SampleRate, 8, sourceFormat.Channels);
        }

        public WaveFormat WaveFormat => _waveFormat;

        public int Read(byte[] buffer, int offset, int bytesCount)
        {
            // How many [SourceBitsPerSample] samples I need to read for reaching [TargetBitsPerSample] bytesCount
            const int ratio = SourceBitsPerSample / TargetBitsPerSample;
            var samplesRequired = bytesCount / ratio;

            //_sourceBuffer = BufferHelpers.Ensure(_sourceBuffer, samplesRequired);
            if (_sourceBuffer == null || _sourceBuffer.Length < samplesRequired)
            {
                _sourceBuffer = new float[samplesRequired];
            }

            var sourceSamples = _sourceProvider.Read(_sourceBuffer, 0, samplesRequired);
            var destWaveBuffer = new WaveBuffer(buffer);
            var destOffset = offset / ratio;

            for (var index = 0; index < sourceSamples; index++)
            {
                var sample = _sourceBuffer[index];
                // sample fits in [-1, 1] so we first add 1 to make it [0, 2];
                var normalizedSample = (sample + 1);
                // multiplying by sbyte.MaxValue to obtain the most significant bits
                destWaveBuffer.ByteBuffer[destOffset++] = (byte)(normalizedSample * sbyte.MaxValue);
            }

            return sourceSamples;
        }
    }
}
