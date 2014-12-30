﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml.Schema;

///
/// Set of classes that contain the Agenda server's XML data model.
///

namespace IndicoInterface
{
    namespace IndicoDataModel
    {
        [Serializable]
        public class iconf
        {
            public string ID; // The ID of the conference.
            public string category; // The category of the actual guy.
            public string title; // Title of the conference.

            public string startDate; // Date when this meeting starts.
            public string endDate; // Date when this meeting ends.

            [XmlElementAttribute("contribution", Form = XmlSchemaForm.Unqualified)]
            public contribution[] contribution; // The contributions.

            [XmlElementAttribute("session", Form = XmlSchemaForm.Unqualified)]
            public session[] session; // Sessions in a conference.

            [XmlElementAttribute("material", Form = XmlSchemaForm.Unqualified)]
            public material[] material; // The material associated with the meeting overall!
        }

        [Serializable]
        public class session
        {
            public string ID; // ID of the session
            public string title; // title of the session.

            public string startDate; // Time when this thing starts
            public string endDate; // When it will end.

            [XmlElementAttribute("contribution", Form = XmlSchemaForm.Unqualified)]
            public contribution[] contribution; // Contributions in this session.

            [XmlElementAttribute("material", Form = XmlSchemaForm.Unqualified)]
            public material[] material; // Material associated directly with this session
        }

        [Serializable]
        public class contribution
        {
            public string ID; // ID of the guy.
            public string title; // Title of presentation.
            public string startDate; // Start time of the presentation.
            public string endDate; // When it is supposed to end.
            [XmlElementAttribute("material", Form = XmlSchemaForm.Unqualified)]
            public material[] material; // The material associated with the stuff!
            [XmlElementAttribute("speakers", Form = XmlSchemaForm.Unqualified)]
            public speaker[] speakers;
            [XmlElementAttribute("subcontribution", Form = XmlSchemaForm.Unqualified)]
            public contribution[] subcontributions;
        }

        [Serializable]
        public class speaker
        {
            [XmlElementAttribute("user", Form = XmlSchemaForm.Unqualified)]
            public userInfo[] users;
        }

        [Serializable]
        public class userInfo
        {
            public userName name;
        }

        [Serializable]
        public class userName
        {
            [XmlAttributeAttribute("first")]
            public string first;
            [XmlAttributeAttribute("middle")]
            public string middle;
            [XmlAttributeAttribute("last")]
            public string last;
        }

        [Serializable]
        public class material
        {
            public string ID; // What is the type
            public string title; // What type of material is this called?
            public string link; // A link to something external for this talk (or whatever).
            public string pdf; // A direct link to a PDF - some agenda servers seem to put this up (!?).
            public string ps; // A direct link to a ps - some agenda servers seem to put this up (!?).
            public string pptx; // A direct link to a pptx - some agenda servers seem to put this up (!?).
            public string ppt; // A direct link to a ppt - some agenda servers seem to put this up (!?).

            public materialFileCollection files; // The file contents
        }

        [Serializable]
        public class materialFileCollection
        {
            [XmlElementAttribute("file", Form = XmlSchemaForm.Unqualified)]
            public materialFile[] file; // The material associated with the stuff!            
        }

        [Serializable]
        public class materialFile
        {
            public string name; // Name of the file.
            public string type; // The type of the file.
            public string url; // URL where we can get the file.
        }
    }
}
