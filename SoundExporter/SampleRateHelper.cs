namespace SoundExporter
{
    public static class SampleRateHelper
    {
        public static int GetSampleRate(SampleRate sampleRate)
        {
            int actualSampleRate;

            switch (sampleRate)
            {
                case SampleRate.High:
                    actualSampleRate = 44100;
                    break;
                default:
                    actualSampleRate = 22050;
                    break;
            }

            return actualSampleRate;
        }
    }
}
