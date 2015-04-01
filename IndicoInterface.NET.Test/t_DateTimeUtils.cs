using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IndicoInterface.NET.Test
{
    [TestClass]
    public class t_DateTimeUtils
    {
        [TestMethod]
        public void TestZero()
        {
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(0, dt.AsSecondsFromUnixEpoch());
        }

        [TestMethod]
        public void TestZeroLocal()
        {
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);
            Assert.AreEqual(-(int)TimeZoneInfo.Local.BaseUtcOffset.TotalSeconds, dt.AsSecondsFromUnixEpoch());
        }

        [TestMethod]
        public void Oct1Utc()
        {
            var dt = new DateTime(1973, 10, 21, 0, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(120009600, dt.AsSecondsFromUnixEpoch());
        }

        [TestMethod]
        public void Oct1Local()
        {
            var dt = new DateTime(1973, 10, 21, 0, 0, 0, DateTimeKind.Local);
            var seconds = dt.AsSecondsFromUnixEpoch();

            // Determine how far off UTC we are, and calc the # of seconds we need to add/subtract to
            // compensate.

            var offset = TimeZoneInfo.Local.BaseUtcOffset;
            seconds += (int)offset.TotalSeconds;
            if (TimeZoneInfo.Local.IsDaylightSavingTime(dt))
            {
                seconds += 60 * 60;
            }

            Assert.AreEqual(120009600, seconds);
        }

        [TestMethod]
        public void ConvertDateTwice()
        {
            var dt = new DateTime(1973, 10, 21, 0, 0, 0, DateTimeKind.Local);
            var seconds = dt.AsSecondsFromUnixEpoch();
            var dtf = seconds.FromUnixTime();
            Assert.AreEqual(dt, dtf);
        }

        [TestMethod]
        public void CompareWithLinuxDateCommand()
        {
            // On Linux (at CERN, lxplus): date -d "Oct 21 1973" +%s
            // output: 120006000
            // Ah - lxplus is doing it for 1973, 10, 21 in local time, and Jan 1, 1760 in UTC time.
            // According to http://www.epochconverter.com/ (!!!). when everything is in UTC time
            // we should be getting: 120009600
            var dt = new DateTime(1973, 10, 21, 0, 0, 0);
            var seconds = 120006000 - 60 * 60; // Account for DST
            var dtf = seconds.FromUnixTime();
            Assert.AreEqual(DateTimeKind.Local, dtf.Kind);
            Assert.AreEqual(dt, dtf);
        }

        [TestMethod]
        public void CompareWithLinuxDateCommandWithDST()
        {
            // Make sure we are doing the calculation correctly when we do the
            // conversion with a day light savings time involved.

            // According to http://www.epochconverter.com/
            var dt = new DateTime(2015, 4, 1, 13, 0, 0, DateTimeKind.Utc);
            var seconds = 1427893200 - 60 * 60; // Account for DST

            // Convert from unix time, and add in the timezone delta to compare above to UTC.
            var dtf = seconds.FromUnixTime();
            Assert.AreEqual(DateTimeKind.Local, dtf.Kind);
            dtf -= TimeZoneInfo.Local.BaseUtcOffset;

            Assert.AreEqual(dt, dtf);
        }

        [TestMethod]
        public void ConvertDateWithDaylightSavings()
        {
            var dt = new DateTime(2015, 4, 1, 13, 0, 0, DateTimeKind.Local);
            var seconds = dt.AsSecondsFromUnixEpoch();
            Assert.AreEqual(1427886000, seconds);
        }
    }
}
