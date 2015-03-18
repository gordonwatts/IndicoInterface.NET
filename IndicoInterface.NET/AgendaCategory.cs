
using System;
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

        static Regex _gConfIdFinderStyle1 = new Regex(@"(?<protocal>http|https)://(?<site>[^/]+)/(?<subdir>.+/)?export/categ/(?<catID>.+)\.ics.*");
        /// <summary>
        /// Create a category token.
        /// </summary>
        /// <param name="categoryUri"></param>
        public AgendaCategory(string categoryUri)
        {
            var m = _gConfIdFinderStyle1.Match(categoryUri);
            if (!m.Success)
            {
                throw new AgendaException(string.Format("Unable to interpret '{0}' as an Indico Category Uri", categoryUri));
            }

            CategoryID = m.Groups["catID"].Value;
            AgendaSite = m.Groups["site"].Value;
            AgendaSubDirectory = m.Groups["subdir"].Value.Replace("/", "");
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
            var m = _gConfIdFinderStyle1.Match(categoryUri);
            return m.Success;
        }

        /// <summary>
        /// Return the URI for a given number of days back in time
        /// </summary>
        /// <param name="daysBeforeToday">We shoudl get the meetings going back how many days in time?</param>
        /// <returns>The URI at which you can load the meeting list iCal</returns>
        public Uri GetCagetoryUri(int daysBeforeToday)
        {
            if (daysBeforeToday < 0)
                throw new ArgumentException("daysBeforeToday must be positive.");

            var b = new StringBuilder();
            b.AppendFormat("https://{0}/", AgendaSite);
            if (!string.IsNullOrWhiteSpace(AgendaSubDirectory))
            {
                b.AppendFormat("{0}/", AgendaSubDirectory);
            }
            b.AppendFormat("export/categ/{0}.ics", CategoryID);

            b.Append("?cookieauth=yes");

            if (daysBeforeToday > 0)
            {
                b.AppendFormat("&from=-{0}d", daysBeforeToday);
            }

            return new Uri(b.ToString());
        }
    }
}
