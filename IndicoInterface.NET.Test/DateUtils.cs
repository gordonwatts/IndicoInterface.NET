using System;

namespace IndicoInterface.NET.Test
{
    static class DateUtils
    {
        public static DateTime ToUtc(this DateTime when)
        {
            var tz = TimeZoneInfo.Local.GetUtcOffset(when);
            return when + tz;
        }
    }
}
