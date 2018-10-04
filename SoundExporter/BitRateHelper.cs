namespace SoundExporter
{
    public static class BitRateHelper
    {
        public static int GetBitRate(BitRate bitRate)
        {
            int actualBitRate;

            switch (bitRate)
            {
                case BitRate.Low:
                    actualBitRate = 8;
                    break;
                default:
                    actualBitRate = 16;
                    break;
            }

            return actualBitRate;
        }
    }
}
