using SetVision.Gamelogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics;

namespace Tests
{
    
    
    /// <summary>
    ///This is a test class for LogicTest and is intended
    ///to contain all LogicTest Unit Tests
    ///</summary>
    [TestClass()]
    public class LogicTest
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
        ///A test for IsSet
        ///</summary>
        [TestMethod()]
        [DeploymentItem("SetVision.exe")]
        public void IsSetTest1()
        {
            Logic_Accessor target = new Logic_Accessor();
            List<Card> possible_set = new List<Card>();
            possible_set.Add(new Card(Color.Red, Shape.Diamond, Fill.Dashed, 1));
            possible_set.Add(new Card(Color.Red, Shape.Diamond, Fill.Dashed, 2));
            possible_set.Add(new Card(Color.Red, Shape.Diamond, Fill.Dashed, 3));
            bool expected = true;
            bool actual;
            actual = target.IsSet(possible_set);
            Assert.AreEqual(expected, actual);

            Logic_Accessor target2 = new Logic_Accessor();
            List<Card> possible_set2 = new List<Card>();
            possible_set2.Add(new Card(Color.Red, Shape.Diamond, Fill.Dashed, 1));
            possible_set2.Add(new Card(Color.Green, Shape.Diamond, Fill.Dashed, 2));
            possible_set2.Add(new Card(Color.Red, Shape.Diamond, Fill.Dashed, 3));
            bool expected2 = false;
            bool actual2;
            actual2 = target2.IsSet(possible_set2);
            Assert.AreEqual(expected2, actual2);
        }

        [TestMethod()]
        [DeploymentItem("SetVision.exe")]
        public void IsSetTest2()
        {
            Logic_Accessor target = new Logic_Accessor();
            List<Card> possible_set = new List<Card>();
            possible_set.Add(new Card(Color.Red, Shape.Diamond, Fill.Dashed, 1));
            possible_set.Add(new Card(Color.Green, Shape.Diamond, Fill.Dashed, 2));
            possible_set.Add(new Card(Color.Red, Shape.Diamond, Fill.Dashed, 3));
            bool expected = false;
            bool actual;
            actual = target.IsSet(possible_set);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for GenerateCards
        ///</summary>
        [TestMethod()]
        public void GenerateCardsTest()
        {
            Logic target = new Logic(); // TODO: Initialize to an appropriate value
            List<Card> cards = target.GenerateCards();

            Assert.AreEqual(81, cards.Count);
        }

        /// <summary>
        ///A test for Logic Constructor
        ///</summary>
        [TestMethod()]
        public void LogicConstructorTest()
        {
            Logic target = new Logic();
            Stopwatch w1 = new Stopwatch();
            w1.Start();
            List<Card> cards = target.GenerateCards();
            w1.Stop();

            Stopwatch w2 = new Stopwatch();
            w2.Start();
            HashSet<List<Card>> all_sets = target.FindSets(cards);
            Assert.AreEqual(1080, all_sets.Count);
            w2.Stop();

            long gen = w1.ElapsedMilliseconds;
            long find = w2.ElapsedMilliseconds;
        }
    }
}
