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
                var outputPath = Path.Join(currentDirectory, outputFileName);
                var thread = new Thread(() =>
                {
                    Debug.WriteLine($"{fileName} - Converting...");

                    try
                    {
                        WavConverter.Convert(file, outputPath, SampleRate.Low, ChannelFormat.Mono, BitRate.Low);
                    }
                    catch (InvalidDataException)
                    {
                        Debug.WriteLine($"{fileName} - Ops, unsupported format");
                    }
                    catch (Exception exception)
                    {
                        Debug.WriteLine($"{fileName} - Ops, {exception.Message}");
                    }
                    finally
                    {
                        Debug.WriteLine($"{fileName} - Done!");
                    }
                });
                thread.Start();
            }
        }
    }
}
