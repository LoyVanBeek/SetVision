using SetVision.Gamelogic.Combinatorics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Tests
{
    
    
    /// <summary>
    ///This is a test class for ListExtensionsTest and is intended
    ///to contain all ListExtensionsTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ListExtensionsTest
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
        ///A test for Combinations
        ///</summary>
        public void CombinationsTestHelper1<T>()
        {
            List<string> orig = new List<string>(new string[]{"A", "B", "C", "D"});//
            int length = 2;
            List<List<string>> expectedList = new List<List<string>>();
                //"AB", "AC", "AD", 
                //"BC", "BD",
                //"CD"});
            expectedList.Add(new List<string>(new string[]{"A","B"}));
            expectedList.Add(new List<string>(new string[]{"A","C"}));
            expectedList.Add(new List<string>(new string[]{"A","D"}));
            expectedList.Add(new List<string>(new string[]{"B","C"}));
            expectedList.Add(new List<string>(new string[]{"B","D"}));
            expectedList.Add(new List<string>(new string[]{"C","D"}));

            List<List<string>> expected = expectedList;
            List<List<string>> actual;
            actual = ListExtensions<string>.Combinations(orig, length);

            for (int i = 0; i < expected.Count - 1; i++)
            {
                List<string> exp = expected[i];
                List<string> act = actual[i];
                CollectionAssert.AreEqual(exp, act);
            }
            //CollectionAssert.AreEqual(expected, actual);
        }
        
        /// <summary>
        ///A test for Combinations
        ///</summary>
        public void CombinationsTestHelper2<T>()
        {
            List<string> orig = new List<string>(new string[] { "A", "B", "C", "D" });//
            int length = 3;
            List<List<string>> expectedList = new List<List<string>>();
            expectedList.Add(new List<string>(new string[] { "A", "B", "C" }));
            expectedList.Add(new List<string>(new string[] { "A", "B", "D" }));
            expectedList.Add(new List<string>(new string[] { "A", "C", "D" }));
            expectedList.Add(new List<string>(new string[] { "B", "C", "D" }));

            List<List<string>> expected = expectedList;
            List<List<string>> actual;
            actual = ListExtensions<string>.Combinations(orig, length);

            for (int i = 0; i < expected.Count - 1; i++)
            {
                List<string> exp = expected[i];
                List<string> act = actual[i];
                CollectionAssert.AreEqual(exp, act);
            }
        }

        /// <summary>
        ///A test for Combinations
        ///</summary>
        public void CombinationsTestHelper3<T>()
        {
            List<string> orig = new List<string>(new string[] { "A", "B", "C", "D", "E" });
            int length = 2;
            List<List<string>> expectedList = new List<List<string>>();

            expectedList.Add(new List<string>(new string[] { "A", "B"}));
            expectedList.Add(new List<string>(new string[] { "A", "C"}));
            expectedList.Add(new List<string>(new string[] { "A", "D"}));
            expectedList.Add(new List<string>(new string[] { "A", "E" }));
            expectedList.Add(new List<string>(new string[] { "B", "C" }));
            expectedList.Add(new List<string>(new string[] { "B", "D" }));
            expectedList.Add(new List<string>(new string[] { "B", "E" }));
            expectedList.Add(new List<string>(new string[] { "C", "D" }));
            expectedList.Add(new List<string>(new string[] { "C", "E" }));
            expectedList.Add(new List<string>(new string[] { "D", "E" }));

            List<List<string>> expected = expectedList;
            List<List<string>> actual;
            actual = ListExtensions<string>.Combinations(orig, length);

            for (int i = 0; i < expected.Count - 1; i++)
            {
                List<string> exp = expected[i];
                List<string> act = actual[i];
                CollectionAssert.AreEqual(exp, act);
            }
        }

        public void CombinationsTestHelper4<T>()
        {
            List<string> orig = new List<string>(new string[] { "A", "B", "C", "D", "E" });
            int length = 3;
            List<List<string>> expectedList = new List<List<string>>();

            expectedList.Add(new List<string>(new string[] { "A", "B", "C" }));
            expectedList.Add(new List<string>(new string[] { "A", "B", "D" }));
            expectedList.Add(new List<string>(new string[] { "A", "B", "E" }));
            expectedList.Add(new List<string>(new string[] { "A", "C", "D" }));
            expectedList.Add(new List<string>(new string[] { "A", "C", "E" }));
            expectedList.Add(new List<string>(new string[] { "A", "D", "E" }));
            expectedList.Add(new List<string>(new string[] { "B", "C", "D" }));
            expectedList.Add(new List<string>(new string[] { "B", "C", "E" }));
            expectedList.Add(new List<string>(new string[] { "B", "D", "E" }));
            expectedList.Add(new List<string>(new string[] { "C", "D", "E" }));

            List<List<string>> expected = expectedList;
            List<List<string>> actual;
            actual = ListExtensions<string>.Combinations(orig, length);

            for (int i = 0; i < expected.Count - 1; i++)
            {
                List<string> exp = expected[i];
                List<string> act = actual[i];
                CollectionAssert.AreEqual(exp, act);
            }
        }

        public void CombinationsTestHelper5<T>()
        {
            List<string> orig = new List<string>(new string[] { "A", "B", "C", "D", "E" });
            int length = 4;
            List<List<string>> expectedList = new List<List<string>>();

            expectedList.Add(new List<string>(new string[] { "A", "B", "C", "D" }));
            expectedList.Add(new List<string>(new string[] { "A", "B", "C", "E" }));
            expectedList.Add(new List<string>(new string[] { "A", "B", "D", "E" }));
            expectedList.Add(new List<string>(new string[] { "A", "C", "D", "E" }));
            expectedList.Add(new List<string>(new string[] { "B", "C", "D", "E" }));

            List<List<string>> expected = expectedList;
            List<List<string>> actual;
            actual = ListExtensions<string>.Combinations(orig, length);

            for (int i = 0; i < expected.Count - 1; i++)
            {
                List<string> exp = expected[i];
                List<string> act = actual[i];
                CollectionAssert.AreEqual(exp, act);
            }
        }

        [TestMethod()]
        public void CombinationsTest()
        {
            CombinationsTestHelper1<char>();
            CombinationsTestHelper2<char>();
            CombinationsTestHelper3<char>();
            CombinationsTestHelper4<char>();
            CombinationsTestHelper5<char>();
        }
    }
}
