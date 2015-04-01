using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace IndicoInterface.NET.Test
{
    public static class utils
    {
        private static DateTime _startOfEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        /// <summary>
        /// Convert the # of seconds into a date/time struct for .NET
        /// </summary>
        /// <param name="secondsSinceEpoch"></param>
        /// <returns>The date represented from the seconds since since Jan 1, 1970 GMT in local time</returns>
        public static DateTime FromUnixTime(this int secondsSinceEpoch)
        {
            var ts = new TimeSpan(0, 0, secondsSinceEpoch);
            var resultUTC = _startOfEpoch + ts;

            // Convert to local time
            var resultLocal = new DateTime(resultUTC.Ticks, DateTimeKind.Local) + TimeZoneInfo.Local.BaseUtcOffset;
            if (TimeZoneInfo.Local.IsDaylightSavingTime(resultLocal))
            {
                resultLocal += new TimeSpan(1, 0, 0);
            }
            return resultLocal;
        }

        /// <summary>
        /// Do a file...
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Tuple<string, string> GetApiAndSecret(string p)
        {
            var f = new FileInfo(p);
            Assert.IsTrue(f.Exists);
            using (var fs = f.OpenText())
            {
                var l1 = fs.ReadLine();
                var l2 = fs.ReadLine();
                return Tuple.Create(l1, l2);
            }
        }
    }
}
