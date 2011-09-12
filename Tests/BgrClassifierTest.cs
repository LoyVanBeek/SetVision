using SetVision.Learning;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Emgu.CV.Structure;
using SetVision.Gamelogic;

namespace Tests
{
    
    
    /// <summary>
    ///This is a test class for BgrClassifierTest and is intended
    ///to contain all BgrClassifierTest Unit Tests
    ///</summary>
    [TestClass()]
    public class BgrClassifierTest
    {


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
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for Classify
        ///</summary>
        [TestMethod()]
        public void ClassifyTest()
        {
            BgrClassifier target = new BgrClassifier();
            Bgr value = new Bgr(145, 110, 197); //Purple
            CardColor actual = target.Classify(value);
            Assert.AreEqual(CardColor.Purple, actual);
        }
    }
}
