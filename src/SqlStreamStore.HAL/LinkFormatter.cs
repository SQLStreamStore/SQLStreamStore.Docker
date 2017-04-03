namespace SqlStreamStore.HAL
{
    internal static class LinkFormatter
    {
        private static string FormatLink(string baseAddress, string direction, int maxCount, long position)
            => $"{baseAddress}?d={direction}&m={maxCount}&p={position}";

        public static string FormatForwardLink(string baseAddress, int maxCount, long position)
            => FormatLink(baseAddress, "f", maxCount, position);

        public static string FormatBackwardLink(string baseAddress, int maxCount, long position)
            => FormatLink(baseAddress, "b", maxCount, position);
    }
}