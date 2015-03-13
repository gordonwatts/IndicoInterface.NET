using System;

namespace IndicoInterface.NET
{
    /// <summary>
    /// Basic info about a meeting (title, etc).
    /// </summary>
    public class AgendaInfoExtended : AgendaInfo
    {
        /// <summary>
        /// Craete a new extended info meeting.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="title"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        public AgendaInfoExtended(string uri, string title, DateTime startTime, DateTime endTime)
            : base(uri)
        {
            Title = title;
            StartTime = startTime;
            EndTime = endTime;
        }

        /// <summary>
        /// Parameterless ctor to be used for serilization.
        /// </summary>
        public AgendaInfoExtended()
        { }

        /// <summary>
        /// Get/Set the title of this meeting
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Get/Set the start time of this meeting (in the local time)
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Get/Set the end time of this meeting (in the local time)
        /// </summary>
        public DateTime EndTime { get; set; }
    }
}
