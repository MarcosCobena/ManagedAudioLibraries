namespace SoundExporter
{
    public static class ChannelFormatHelper
    {
        public static int GetChannelsCount(ChannelFormat channelFormat)
        {
            int count;

            switch (channelFormat)
            {
                case ChannelFormat.Mono:
                    count = 1;
                    break;
                default:
                    count = 2;
                    break;
            }

            return count;
        }
    }
}
