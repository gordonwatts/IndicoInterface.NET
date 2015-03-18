using System;

namespace IndicoInterface.NET.Test
{
    public static class utils
    {
        private static DateTime _startOfEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        /// <summary>
        /// Convert the # of seconds into a date/time struct for .NET
        /// </summary>
        /// <param name="secondsSinceEpoch"></param>
        /// <returns></returns>
        public static DateTime FromUnixTime(this int secondsSinceEpoch)
        {
            var ts = new TimeSpan(0, 0, secondsSinceEpoch);
            return _startOfEpoch + ts;
        }
    }
}
