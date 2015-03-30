
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
namespace IndicoInterface.NET
{
    /// <summary>
    /// Token that contains a reference to an agenda category (which often contains talks).
    /// </summary>
    public class AgendaCategory
    {
        /// <summary>
        /// The main URL of the conference.
        /// </summary>
        public string CategoryID { get; set; }

        /// <summary>
        /// The web site where the agenda is located.
        /// </summary>
        public string AgendaSite { get; set; }

        /// <summary>
        /// Returns the agenda sub directory
        /// </summary>
        public string AgendaSubDirectory { get; set; }

        static Regex[] _gConfIdFinder = new Regex[] {
            new Regex(@"(?<protocal>http|https)://(?<site>[^/]+)/(?<subdir>.+/)?export/categ/(?<catID>.+)\.ics.*"),
            new Regex(@"(?<protocal>http|https)://(?<site>[^/]+)/(?<subdir>.+/)?category/(?<catID>[^/]+)/*"),
            new Regex(@"(?<protocal>http|https)://(?<site>[^/]+)/(?<subdir>.+/)?.*categId=(?<catID>[^&/]+).*"),
        };

        /// <summary>
        /// Create a category token.
        /// </summary>
        /// <param name="categoryUri"></param>
        public AgendaCategory(string categoryUri)
        {
            var m = FindConfMatch(categoryUri);
            if (m == null)
            {
                throw new AgendaException(string.Format("Unable to interpret '{0}' as an Indico Category Uri", categoryUri));
            }

            CategoryID = m.Groups["catID"].Value;
            AgendaSite = m.Groups["site"].Value;
            AgendaSubDirectory = m.Groups["subdir"].Value.Replace("/", "");
        }

        /// <summary>
        /// Look through all the styles and see if anything matches what we need
        /// </summary>
        /// <param name="categoryUri"></param>
        /// <returns></returns>
        private static Match FindConfMatch(string categoryUri)
        {
            return _gConfIdFinder
                .Select(x => x.Match(categoryUri))
                .Where(m => m.Success)
                .FirstOrDefault();
        }

        /// <summary>
        /// Parameterless ctor to be used for serilization.
        /// </summary>
        public AgendaCategory()
        { }

        /// <summary>
        /// Is the URI a valid category URI? It doesn't test to make sure the category actually exists on the remote machine!
        /// </summary>
        /// <param name="categoryUri">URI to test</param>
        /// <returns>True if this is a valid category URI</returns>
        public static bool IsValid(string categoryUri)
        {
            var m = FindConfMatch(categoryUri);
            return m != null;
        }

        /// <summary>
        /// Return the URI for a given number of days back in time
        /// </summary>
        /// <param name="daysBeforeToday">We shoudl get the meetings going back how many days in time?</param>
        /// <param name="apiKey">The API key to use (or null if it doesn't exist)</param>
        /// <param name="secret">The api key's secret to use (or null if it doesn't exist)</param>
        /// <returns>The URI at which you can load the meeting list iCal</returns>
        public Uri GetCagetoryUri(int daysBeforeToday, string apiKey = null, string secret = null, bool useTimestamp = true)
        {
            if (daysBeforeToday < 0)
                throw new ArgumentException("daysBeforeToday must be positive.");

            // Build the path portion of the argument, including any encoding needed.
            var urlParams = new Dictionary<string, string>();

            if (daysBeforeToday > 0)
            {
                urlParams.Add("from", string.Format("-{0}d", daysBeforeToday));
            }

            var pathStub = ApiKeyHandler.IndicoEncode(string.Format("/export/categ/{0}.ics", CategoryID), urlParams, apiKey, secret, useTimeStamp: useTimestamp);

            // Put everything together
            var b = new StringBuilder();
            b.AppendFormat("https://{0}", AgendaSite);
            if (!string.IsNullOrWhiteSpace(AgendaSubDirectory))
            {
                b.AppendFormat("/{0}", AgendaSubDirectory);
            }
            b.Append(pathStub);

            // And return as a URI.

            return new Uri(b.ToString());
        }
    }
}
