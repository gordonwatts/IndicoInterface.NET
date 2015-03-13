using iCal.PCL.DataModel;
using iCal.PCL.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IndicoInterface.NET
{
    static class iCalUtils
    {
        /// <summary>
        /// Convert an incoming stream into iCalEvent's.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<iCalVEvent>> iCalFromStream(this StreamReader s)
        {
            return iCalSerializer.Deserialize(await s.AsLines())
                .Where(x => x is iCalVEvent)
                .Cast<iCalVEvent>();
        }

        /// <summary>
        /// Return the lines as an array
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static async Task<string[]> AsLines(this StreamReader s)
        {
            var lines = new List<string>();
            string line = null;
            do
            {
                line = await s.ReadLineAsync();
                if (line != null)
                    lines.Add(line);
            } while (line != null);

            return lines.ToArray();
        }
    }
}
