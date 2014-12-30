using IndicoInterface.SimpleAgendaDataModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace t_IndicoInterface
{
    /// <summary>
    /// Summary description for t_SimpleAgendaDataModel
    /// </summary>
    [TestClass]
    public class t_SimpleAgendaDataModel
    {
        public t_SimpleAgendaDataModel()
        {
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

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
            Assert.IsFalse(t1 == t2, "The talks should not be euqal, but they are!");
            Assert.IsTrue(t1 != t2, "The talks slide url are not equal, should have != been true!");
        }
    }
}
