using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

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
    }
}
