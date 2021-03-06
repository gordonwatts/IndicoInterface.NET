﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Xml.Serialization;

namespace IndicoInterface.NET.Test
{
    [TestClass]
    public class t_AgendaCategory
    {
        [TestMethod]
        public void LegalCategoryURIWithFrom()
        {
            var c = new AgendaCategory("https://indico.cern.ch/export/categ/2636.ics?from=-14d");
            Assert.AreEqual("indico.cern.ch", c.AgendaSite);
            Assert.AreEqual("", c.AgendaSubDirectory);
            Assert.AreEqual("2636", c.CategoryID);
        }

        [TestMethod]
        public void LegalCategoryURIPlain()
        {
            var c = new AgendaCategory("https://indico.cern.ch/export/categ/2636.ics");
            Assert.AreEqual("indico.cern.ch", c.AgendaSite);
            Assert.AreEqual("", c.AgendaSubDirectory);
            Assert.AreEqual("2636", c.CategoryID);
        }

        [TestMethod]
        public void LegacyCategoryURIPlain()
        {
            var c = new AgendaCategory("https://indico.fnal.gov/categoryDisplay.py?categId=22");
            Assert.AreEqual("indico.fnal.gov", c.AgendaSite);
            Assert.AreEqual("", c.AgendaSubDirectory);
            Assert.AreEqual("22", c.CategoryID);
        }

        [TestMethod]
        public void LegacyCategoryURISubSite()
        {
            var c = new AgendaCategory("https://indico.fnal.gov/bogus/categoryDisplay.py?categId=22");
            Assert.AreEqual("indico.fnal.gov", c.AgendaSite);
            Assert.AreEqual("bogus", c.AgendaSubDirectory);
            Assert.AreEqual("22", c.CategoryID);
        }

        [TestMethod]
        public void LegacyCategoryURIArgsAfter()
        {
            var c = new AgendaCategory("https://indico.fnal.gov/bogus/categoryDisplay.py?categId=22&from=-7d");
            Assert.AreEqual("indico.fnal.gov", c.AgendaSite);
            Assert.AreEqual("bogus", c.AgendaSubDirectory);
            Assert.AreEqual("22", c.CategoryID);
        }

        [TestMethod]
        public void LegacyCategoryURIArgsBefore()
        {
            var c = new AgendaCategory("https://indico.fnal.gov/bogus/categoryDisplay.py?from=-7d&categId=22");
            Assert.AreEqual("indico.fnal.gov", c.AgendaSite);
            Assert.AreEqual("bogus", c.AgendaSubDirectory);
            Assert.AreEqual("22", c.CategoryID);
        }

        [TestMethod]
        public void LegalCategoryFromDefaultURL()
        {
            var c = new AgendaCategory("https://indico.cern.ch/category/3286/");
            Assert.AreEqual("indico.cern.ch", c.AgendaSite);
            Assert.AreEqual("", c.AgendaSubDirectory);
            Assert.AreEqual("3286", c.CategoryID);
        }

        [TestMethod]
        public void LegalCategoryFromDefaultURLWithSubDir()
        {
            var c = new AgendaCategory("https://indico.cern.ch/bogus/category/3286/");
            Assert.AreEqual("indico.cern.ch", c.AgendaSite);
            Assert.AreEqual("bogus", c.AgendaSubDirectory);
            Assert.AreEqual("3286", c.CategoryID);
        }

        [TestMethod]
        public void LegalCategoryURISubDir()
        {
            var c = new AgendaCategory("https://indico.cern.ch/special/export/categ/2636.ics");
            Assert.AreEqual("indico.cern.ch", c.AgendaSite);
            Assert.AreEqual("special", c.AgendaSubDirectory);
            Assert.AreEqual("2636", c.CategoryID);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void LookForNegativeDaysBackUri()
        {
            var c = new AgendaCategory("https://indico.cern.ch/special/export/categ/2636.ics");
            var uri = c.GetCagetoryUri(-1);
        }

        [TestMethod]
        public void LookForPlaneUri()
        {
            var c = new AgendaCategory("https://indico.cern.ch/export/categ/2636.ics?from=-14d");
            var uri = c.GetCagetoryUri(0);
            Assert.AreEqual("https://indico.cern.ch/export/categ/2636.ics", uri.OriginalString);
        }

        [TestMethod]
        public void LookForPlaneUriWithApiKey()
        {
            var c = new AgendaCategory("https://indico.cern.ch/export/categ/2636.ics?from=-14d");
            var uri = c.GetCagetoryUri(0, "00000000-0000-0000-0000-000000000000");
            Assert.AreEqual("https://indico.cern.ch/export/categ/2636.ics?apikey=00000000-0000-0000-0000-000000000000", uri.OriginalString);
        }

        [TestMethod]
        public void LookForPlaneUriWithSubDir()
        {
            var c = new AgendaCategory("https://indico.cern.ch/special/export/categ/2636.ics?from=-14d");
            var uri = c.GetCagetoryUri(0);
            Assert.AreEqual("https://indico.cern.ch/special/export/categ/2636.ics", uri.OriginalString);
        }

        [TestMethod]
        public void LookFor60DaysBackUri()
        {
            var c = new AgendaCategory("https://indico.cern.ch/special/export/categ/2636.ics");
            var uri = c.GetCagetoryUri(60);
            Assert.IsTrue(uri.OriginalString.Contains("from=-60d"));
        }

        [TestMethod]
        public void LookFor120DaysBackUri()
        {
            var c = new AgendaCategory("https://indico.cern.ch/special/export/categ/2636.ics");
            var uri = c.GetCagetoryUri(120);
            Assert.IsTrue(uri.OriginalString.Contains("from=-120d"));
        }

        [TestMethod]
        public void ParseSecretUri()
        {
            var c = new AgendaCategory("https://indico.cern.ch/export/categ/2636.ics?apikey=00000000-0000-0000-0000-000000000000&from=-7d&signature=000a000aa0a0a0a000a000a0a00aaa00a0a0000a");
            Assert.AreEqual("indico.cern.ch", c.AgendaSite);
            Assert.AreEqual("", c.AgendaSubDirectory);
            Assert.AreEqual("2636", c.CategoryID);
        }

        [TestMethod]
        [ExpectedException(typeof(AgendaException))]
        public void ThrowWithAgendaInfo()
        {
            var c = new AgendaCategory("http://indico.fnal.gov/bogus/conferenceTimeTable.py?confId=1829");
        }

        [TestMethod]
        public void IsValidTest()
        {
            Assert.IsTrue(AgendaCategory.IsValid("https://indico.cern.ch/export/categ/2636.ics?from=-14d"));
            Assert.IsTrue(AgendaCategory.IsValid("https://indico.cern.ch/export/categ/2636.ics?apikey=00000000-0000-0000-0000-000000000000&from=-7d&signature=000a000aa0a0a0a000a000a0a00aaa00a0a0000a"));
            Assert.IsTrue(AgendaCategory.IsValid("https://indico.cern.ch/special/export/categ/2636.ics"));
            Assert.IsTrue(AgendaCategory.IsValid("https://indico.cern.ch/export/categ/2636.ics"));
            Assert.IsFalse(AgendaCategory.IsValid("http://indico.fnal.gov/bogus/conferenceTimeTable.py?confId=1829"));
            Assert.IsTrue(AgendaCategory.IsValid("https://indico.cern.ch/export/categ/2636.ics"));
        }

        [TestMethod]
        public void SerializeXML()
        {
            // Serialize and de-serilize agenda info

            var ai = new AgendaCategory("http://indico.cern.ch/export/categ/1234.ics");

            var x = new XmlSerializer(typeof(AgendaCategory));
            using (var m = new MemoryStream())
            {
                x.Serialize(m, ai);

                m.Seek(0, SeekOrigin.Begin);
                var o = x.Deserialize(m) as AgendaCategory;

                Assert.IsNotNull(o);
                Assert.AreEqual(ai.AgendaSite, o.AgendaSite);
                Assert.AreEqual(ai.AgendaSubDirectory, o.AgendaSubDirectory);
                Assert.AreEqual(ai.CategoryID, o.CategoryID);
            }
        }

        [TestMethod]
        public void SerializeJSON()
        {
            // Serialize and de-serilize agenda info

            var ai = new AgendaCategory("http://indico.cern.ch/export/categ/1234.ics");

            var json = JsonConvert.SerializeObject(ai);
            var o1 = JsonConvert.DeserializeObject<AgendaCategory>(json);
            Assert.IsNotNull(o1);
            var o = o1 as AgendaCategory;

            Assert.IsNotNull(o);
            Assert.AreEqual(ai.AgendaSite, o.AgendaSite);
            Assert.AreEqual(ai.AgendaSubDirectory, o.AgendaSubDirectory);
            Assert.AreEqual(ai.CategoryID, o.CategoryID);
        }
    }
}
