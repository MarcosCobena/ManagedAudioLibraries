using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using SoundExporter;

namespace SoundExporterPlayground
{
    class Program
    {
        static void Main(string[] args)
        {
            var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var inputFiles = Directory.EnumerateFiles(currentDirectory, "*.wav", SearchOption.TopDirectoryOnly);

            foreach (var file in inputFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var outputFileName = $"{fileName}-out.wav";
                var outputPath = Path.Combine(currentDirectory, "Output", outputFileName);
                var thread = new Thread(() =>
                {
                    Debug.WriteLine($"Converting {fileName}...");

                    try
                    {
                        WavConverter.Convert(file, outputPath, SampleRate.Low, ChannelFormat.Mono, BitRate.Low);
                    }
                    catch (InvalidDataException)
                    {
                        Debug.WriteLine($":-S Ops, unsupported format: {fileName}");
                    }
                    catch (Exception exception)
                    {
                        Debug.WriteLine($":,-( Ops, {exception.Message}: {fileName}");
                    }
                    finally
                    {
                        Debug.WriteLine($"Done!: {fileName}");
                    }
                });
                thread.Start();
            }
        }
    }
}
