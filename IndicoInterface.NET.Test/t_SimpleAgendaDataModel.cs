using IndicoInterface.NET.SimpleAgendaDataModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace t_IndicoInterface
{
    /// <summary>
    /// Summary description for t_SimpleAgendaDataModel
    /// </summary>
    [TestClass]
    public class t_SimpleAgendaDataModel
    {
        [TestMethod]
        public void TestTalkEquivalence()
        {
            Talk t1 = new Talk();
            Talk t2 = new Talk();
            t1.ID = "s1234";
            t2.ID = "s1234";

            t1.SlideURL = "hithere";
            t2.SlideURL = "hithere";

            Assert.IsTrue(t1.Equals(t2), "Expected explicit call to Equals to show things are the same!");
            bool temp = t1 == t2;
            Assert.IsTrue(t1 == t2, "The talks should be equal, but they aren't");
            Assert.IsFalse(t1 != t2, "The talks are equal, so != should have failed!");

            t1.SlideURL = "bogus";
            Assert.IsFalse(t1 == t2, "The talks should not be equal, but they are!");
            Assert.IsTrue(t1 != t2, "The talks are not equal, so != should have been fine!");

            t1.SlideURL = t2.SlideURL;
            t1.ID = "freak";
            Assert.IsFalse(t1 == t2, "The talks should not be equal, but they are!");
            Assert.IsTrue(t1 != t2, "The talks slide URL are not equal, should have != been true!");
        }
    }
}
