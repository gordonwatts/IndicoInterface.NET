using System;

namespace IndicoInterface.NET
{
    public static class DateTimeUtils
    {
        /// <summary>
        /// Return the date time as the number of seconds since Jan 1, 1970.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static int AsSecondsFromUnixEpoch(this DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Unspecified)
                throw new ArgumentException("Don't know how to shift timezones for an unspecified DateTime kind!");

            // We have to deal with time zones here.
            var dtUtc = dt;
            if (dt.Kind == DateTimeKind.Local)
            {
                var offset = TimeZoneInfo.Local.BaseUtcOffset;
                dtUtc = new DateTime(dt.Ticks, DateTimeKind.Local) - offset;
            }

            var ts = dtUtc - new DateTime(1970, 1, 1, 0, 0, 0);
            return (int)ts.TotalSeconds;
        }
    }
}
