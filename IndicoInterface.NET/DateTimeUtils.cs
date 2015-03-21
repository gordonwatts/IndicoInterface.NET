using System;

namespace IndicoInterface.NET
{
    static class DateTimeUtils
    {
        /// <summary>
        /// Return the date time as the number of seconds since Jan 1, 1970.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static int AsSecondsFromUnixEpoch(this DateTime dt)
        {
            var ts = dt - new DateTime(1970, 1, 1);
            return (int)ts.TotalSeconds;
        }
    }
}
