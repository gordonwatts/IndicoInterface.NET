using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Xml.Serialization;

namespace IndicoInterface.NET.Test
{
    [TestClass]
    public class t_AgendaInfoExtended
    {
        [TestMethod]
        public void SerializeAgendaInfoExtendedXML()
        {
            // Serialize and de-serilize agenda info

            var ai = new AgendaInfoExtended("http://indico.cern.ch/event/1234", "this is me", DateTime.Now, DateTime.Now);

            var x = new XmlSerializer(typeof(AgendaInfoExtended));
            using (var m = new MemoryStream())
            {
                x.Serialize(m, ai);

                m.Seek(0, SeekOrigin.Begin);
                var o = x.Deserialize(m) as AgendaInfoExtended;

                Assert.IsNotNull(o);
                Assert.AreEqual(ai.AgendaSite, o.AgendaSite);
                Assert.AreEqual(ai.AgendaSubDirectory, o.AgendaSubDirectory);
                Assert.AreEqual(ai.ConferenceID, o.ConferenceID);
                Assert.AreEqual(ai.ConferenceUrl, o.ConferenceUrl);
                Assert.AreEqual(ai.EndTime, o.EndTime);
                Assert.AreEqual(ai.StartTime, o.StartTime);
                Assert.AreEqual(ai.Title, o.Title);
            }
        }

        [TestMethod]
        public void SerializeAgendaInfoExtendedJSON()
        {
            // Serialize and de-serilize agenda info

            var ai = new AgendaInfoExtended("http://indico.cern.ch/event/1234", "this is me", DateTime.Now, DateTime.Now);

            var json = JsonConvert.SerializeObject(ai);
            var o1 = JsonConvert.DeserializeObject<AgendaInfoExtended>(json);
            Assert.IsNotNull(o1);
            var o = o1 as AgendaInfoExtended;

            Assert.IsNotNull(o);
            Assert.AreEqual(ai.AgendaSite, o.AgendaSite);
            Assert.AreEqual(ai.AgendaSubDirectory, o.AgendaSubDirectory);
            Assert.AreEqual(ai.ConferenceID, o.ConferenceID);
            Assert.AreEqual(ai.ConferenceUrl, o.ConferenceUrl);
            Assert.AreEqual(ai.EndTime, o.EndTime);
            Assert.AreEqual(ai.StartTime, o.StartTime);
            Assert.AreEqual(ai.Title, o.Title);
        }
    }
}
