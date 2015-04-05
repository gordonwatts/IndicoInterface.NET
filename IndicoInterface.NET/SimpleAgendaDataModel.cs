using System;
using System.Text;

///
/// Simple set of classes to hold onto a meeting or conference
/// and its info. Mainly so that we can get rid of any thing that has to
/// do with the difference between agenda servers and types of meetings
/// (workshop vs. meeting).
/// 

namespace IndicoInterface.NET
{
    namespace SimpleAgendaDataModel
    {
        /// <summary>
        /// The top level meeting class
        /// </summary>
        public class Meeting
        {
            public string ID; // Conference ID
            public string Title; // Conference title

            public string Site; // Site where the agenda is stored.

            public DateTime StartDate; // When this meeting started
            public DateTime EndDate; // When this meeting finished.

            /// <summary>
            /// Sessions in this meeting/conference. A normal boring meeting will have only a single session.
            /// </summary>
            public Session[] Sessions;

            /// <summary>
            /// The PDF files, etc., that are associated directly with the Meeting (attached to the meeting header).
            /// </summary>
            public Talk[] MeetingTalks;
        }

        public class Session
        {
            public string ID;
            public string Title; // Title of the session.
            public DateTime StartDate; // When this meeting started
            public DateTime EndDate; // When this meeting finished.
            public Talk[] Talks; // Talks in this session
            public Talk[] SessionMaterial; // Some material that was attached to the session
        }

        public enum TypeOfTalk
        {
            Talk, ExtraMaterial
        }

        /// <summary>
        /// Information about the material that a talk contains.
        /// </summary>
        public class TalkMaterial
        {
            /// <summary>
            /// The URL to download the material
            /// </summary>
            public string URL;

            /// <summary>
            /// The filename associated with the material
            /// </summary>
            public string DisplayFilename;

            /// <summary>
            /// The extension associated with the file that was uploaded.
            /// </summary>
            public string FilenameExtension;

            /// <summary>
            /// The material type (slides, etc.)
            /// </summary>
            public string MaterialType;
        }

        public class Talk
        {
            public string ID;
            public string Title;
            public DateTime StartDate;
            public DateTime EndDate;
            public string[] Speakers;
            public Talk[] SubTalks; // Any sub-talks we have going for us
            public TypeOfTalk TalkType;

            /// <summary>
            /// The slide URL for the default material for the talk
            /// </summary>
            public string SlideURL;

            /// <summary>
            /// The filename one can use to display the default material to the user.
            /// </summary>
            public string DisplayFilename; // A filename that looks good to the user

            /// <summary>
            /// THe extension of the filename for the default file. Includes the period (.pdf).
            /// </summary>
            public string FilenameExtension;

            /// <summary>
            /// All material directly attached to this talk.
            /// </summary>
            /// <remarks>This will include multiple versions of the Slides (pdf, pptx), etc.</remarks>
            public TalkMaterial[] AllMaterial;

            /// <summary>
            /// Custom string method to help with debugging and the like.
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                StringBuilder bld = new StringBuilder();
                bld.Append("Talk ID=" + ID + " (" + Title + ") - ");
                bld.Append(SlideURL == null ? "no URL" : SlideURL);
                return bld.ToString();
            }

            /// <summary>
            /// Returns true if these guys are the same
            /// </summary>
            /// <param name="t1"></param>
            /// <param name="t2"></param>
            /// <returns></returns>
            public static bool operator ==(Talk t1, Talk t2)
            {
                object ot1 = t1;
                object ot2 = t2;

                if (null == ot1
                    || null == ot2)
                {
                    return null == ot1
                        && null == ot2;
                }
                return t1.ID == t2.ID
                    && t1.SlideURL == t2.SlideURL;
            }

            /// <summary>
            /// And the != operator.
            /// </summary>
            /// <param name="t1"></param>
            /// <param name="t2"></param>
            /// <returns></returns>
            public static bool operator !=(Talk t1, Talk t2)
            {
                return !(t1 == t2);
            }

            /// <summary>
            /// Equal is based on ID and slide URL only.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                Talk t2 = obj as Talk;
                if (t2 == null)
                {
                    return false;
                }
                return this == t2;
            }

            /// <summary>
            /// Return a hash code based solely on the ID and URL
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return (ID + SlideURL).GetHashCode();
            }
        }
    }
}
