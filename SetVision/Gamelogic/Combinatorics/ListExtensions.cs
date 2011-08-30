using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace SetVision.Gamelogic.Combinatorics
{
    public static class ListExtensions<T>
    {
        #region py
    //    def combinations(orig, length):
    //# combinations('ABCD', 2) --> AB AC AD BC BD CD
    //# combinations(range(4), 3) --> 012 013 023 123
    //orig = tuple(iterable)
    //orig.count = len(pool)
    //if length > n:
    //    return
    //indices = range(length)
    //yield tuple(pool[i] for i in indices)
    //while True:
    //    for i in reversed(range(length)):
    //        if indices[i] != i + orig.count - length:
    //            break
    //    else:
    //        return
    //    indices[i] += 1
    //    for j in range(i+1, length):
    //        indices[j] = indices[j-1] + 1
    //    yield tuple(pool[i] for i in indices)
        #endregion py

        //public static IEnumerator<List<T>> Combinations(this List<T> orig, int length)
        //{
        //    if (length > orig.Count)
        //    {
        //        throw new ArgumentException("Combination length longer than available elements");
        //    }

        //    //indices = range(length)
        //    List<int> indices = range(length);

        //    //yield tuple(pool[i] for i in indices)
        //    List<T> forIndices = new List<T>(length);
        //    foreach (int index in indices)
        //    {
        //        forIndices.Add(orig[index]);
        //    }
        //    yield return forIndices;

        //    //while True:
        //    while (true)
        //    {
        //        //    for i in reversed(range(length)):
        //        List<int> reversed = range(length).Reverse();
        //        foreach (int i in reversed)
        //        {
        //            if (indices[i] != (i + orig.Count - length))
        //            {
        //                //            break
        //                break;
        //            }
        //        }
        //    }
        //}

        public static List<List<T>> Combinations(List<T> orig, int length)
        {
            List<List<T>> combinations = new List<List<T>>();
            List<T> copy = new List<T>(orig);
            if (length > 2)
            {
                for (int i = 0; i < copy.Count - 2; i++)
                {
                    T el = copy[i];
                    List<T> rest = copy.GetRange(i+1, copy.Count - i - 1);
                    List<List<T>> subs = Combinations(rest, length - 1);
                    foreach (List<T> sub in subs)
                    {
                        sub.Insert(0, el);
                        combinations.Add(sub);
                    }
                }
            }
            else
            {
                return Combinations(orig);
            }
            return combinations;
        }

        public static List<List<T>> Combinations(List<T> orig)
        {
            //combinations('ABCD', 2) --> AB AC AD BC BD CD
            //Combine the first element with the other elements:
            //  el1 = copy[0];
            //Then append Combinations(copy-el1, length-1);

            List<List<T>> combinations = new List<List<T>>();
            List<T> copy = new List<T>(orig);

            if (copy.Count == 2)
            {
                combinations.Add(copy);
            }
            else if (copy.Count > 0)
            {
                T head = copy[0];
                copy.RemoveAt(0);

                foreach(T part in copy)
                {
                    List<T> combi = new List<T>();
                    combi.Add(head);
                    combi.Add(part);

                    combinations.Add(combi);
                }

                List<List<T>> sub = Combinations(copy);
                combinations.AddRange(sub);
            }
            return combinations;
        }

        private static List<int> range(int length)
        {
            List<int> l = new List<int>(length);
            for (int i = 0; i <= length; i++)
            {
                l.Add(i);
            }
            return l;
        }
    }

    //interface IMetaCollection<T> : IEnumerable<IList<T>>
    //{
    //    /// <summary>
    //    /// The count of items in the collection.  This is not inherited from
    //    /// ICollection since this meta-collection cannot be extended by users.
    //    /// </summary>
    //    long Count { get; }

    //    /// <summary>
    //    /// The type of the meta-collection, determining how the collections are 
    //    /// determined from the inputs.
    //    /// </summary>
    //    GenerateOption Type { get; }

    //    /// <summary>
    //    /// The upper index of the meta-collection, which is the size of the input collection.
    //    /// </summary>
    //    int UpperIndex { get; }

    //    /// <summary>
    //    /// The lower index of the meta-collection, which is the size of each output collection.
    //    /// </summary>
    //    int LowerIndex { get; }
    //}

    //public enum GenerateOption
    //{
    //    /// <summary>
    //    /// Do not generate additional sets, typical implementation.
    //    /// </summary>
    //    WithoutRepetition,
    //    /// <summary>
    //    /// Generate additional sets even if repetition is required.
    //    /// </summary>
    //    WithRepetition
    //}

    //public class Permutations<T> : IMetaCollection<T>
    //{

    //    #region Constructors

    //    /// <summary>
    //    /// No default constructor, must at least provided a list of values.
    //    /// </summary>
    //    protected Permutations()
    //    {
    //        ;
    //    }

    //    /// <summary>
    //    /// Create a permutation set from the provided list of values.  
    //    /// The values (T) must implement IComparable.  
    //    /// If T does not implement IComparable use a constructor with an explict IComparer.
    //    /// The repetition type defaults to MetaCollectionType.WithholdRepetitionSets
    //    /// </summary>
    //    /// <param name="values">List of values to permute.</param>
    //    public Permutations(IList<T> values)
    //    {
    //        Initialize(values, GenerateOption.WithoutRepetition, null);
    //    }

    //    /// <summary>
    //    /// Create a permutation set from the provided list of values.  
    //    /// If type is MetaCollectionType.WithholdRepetitionSets, then values (T) must implement IComparable.  
    //    /// If T does not implement IComparable use a constructor with an explict IComparer.
    //    /// </summary>
    //    /// <param name="values">List of values to permute.</param>
    //    /// <param name="type">The type of permutation set to calculate.</param>
    //    public Permutations(IList<T> values, GenerateOption type)
    //    {
    //        Initialize(values, type, null);
    //    }

    //    /// <summary>
    //    /// Create a permutation set from the provided list of values.  
    //    /// The values will be compared using the supplied IComparer.
    //    /// The repetition type defaults to MetaCollectionType.WithholdRepetitionSets
    //    /// </summary>
    //    /// <param name="values">List of values to permute.</param>
    //    /// <param name="comparer">Comparer used for defining the lexigraphic order.</param>
    //    public Permutations(IList<T> values, IComparer<T> comparer)
    //    {
    //        Initialize(values, GenerateOption.WithoutRepetition, comparer);
    //    }

    //    #endregion

    //    #region IEnumerable Interface

    //    /// <summary>
    //    /// Gets an enumerator for collecting the list of permutations.
    //    /// </summary>
    //    /// <returns>The enumerator.</returns>
    //    public virtual IEnumerator<T> GetEnumerator()
    //    {
    //        return new Enumerator<T>(this);
    //    }

    //    /// <summary>
    //    /// Gets an enumerator for collecting the list of permutations.
    //    /// </summary>
    //    /// <returns>The enumerator.</returns>
    //    IEnumerator<IList<T>> IEnumerable<IList<T>>.GetEnumerator()
    //    {
    //        return new Enumerator(this);
    //    }

    //    #endregion

    //    #region Enumerator Inner-Class

    //    /// <summary>
    //    /// The enumerator that enumerates each meta-collection of the enclosing Permutations class.
    //    /// </summary>
    //    public class Enumerator : IEnumerator<IList<T>>
    //    {

    //        #region Constructors

    //        /// <summary>
    //        /// Construct a enumerator with the parent object.
    //        /// </summary>
    //        /// <param name="source">The source Permutations object.</param>
    //        public Enumerator(Permutations<T> source)
    //        {
    //            myParent = source;
    //            myLexicographicalOrders = new int[source.myLexicographicOrders.Length];
    //            source.myLexicographicOrders.CopyTo(myLexicographicalOrders, 0);
    //            Reset();
    //        }

    //        #endregion

    //        #region IEnumerator Interface

    //        /// <summary>
    //        /// Resets the permutations enumerator to the first permutation.  
    //        /// This will be the first lexicographically order permutation.
    //        /// </summary>
    //        public void Reset()
    //        {
    //            myPosition = Position.BeforeFirst;
    //        }

    //        /// <summary>
    //        /// Advances to the next permutation.
    //        /// </summary>
    //        /// <returns>True if successfully moved to next permutation, False if no more permutations exist.</returns>
    //        /// <remarks>
    //        /// Continuation was tried (i.e. yield return) by was not nearly as efficient.
    //        /// Performance is further increased by using value types and removing generics, that is, the LexicographicOrder parellel array.
    //        /// This is a issue with the .NET CLR not optimizing as well as it could in this infrequently used scenario.
    //        /// </remarks>
    //        public bool MoveNext()
    //        {
    //            if (myPosition == Position.BeforeFirst)
    //            {
    //                myValues = new List<T>(myParent.myValues.Count);
    //                myValues.AddRange(myParent.myValues);
    //                Array.Sort(myLexicographicalOrders);
    //                myPosition = Position.InSet;
    //            }
    //            else if (myPosition == Position.InSet)
    //            {
    //                if (myValues.Count < 2)
    //                {
    //                    myPosition = Position.AfterLast;
    //                }
    //                else if (NextPermutation() == false)
    //                {
    //                    myPosition = Position.AfterLast;
    //                }
    //            }
    //            return myPosition != Position.AfterLast;
    //        }

    //        /// <summary>
    //        /// The current permutation.
    //        /// </summary>
    //        public object Current
    //        {
    //            get
    //            {
    //                if (myPosition == Position.InSet)
    //                {
    //                    return new List<T>(myValues);
    //                }
    //                else
    //                {
    //                    throw new InvalidOperationException();
    //                }
    //            }
    //        }

    //        /// <summary>
    //        /// The current permutation.
    //        /// </summary>
    //        IList<T> IEnumerator<IList<T>>.Current
    //        {
    //            get
    //            {
    //                if (myPosition == Position.InSet)
    //                {
    //                    return new List<T>(myValues);
    //                }
    //                else
    //                {
    //                    throw new InvalidOperationException();
    //                }
    //            }
    //        }

    //        /// <summary>
    //        /// Cleans up non-managed resources, of which there are none used here.
    //        /// </summary>
    //        public virtual void Dispose()
    //        {
    //            ;
    //        }

    //        #endregion

    //        #region Heavy Lifting Methods

    //        /// <summary>
    //        /// Calculates the next lexicographical permutation of the set.
    //        /// This is a permutation with repetition where values that compare as equal will not 
    //        /// swap positions to create a new permutation.
    //        /// http://www.cut-the-knot.org/do_you_know/AllPerm.shtml
    //        /// E. W. Dijkstra, A Discipline of Programming, Prentice-Hall, 1997  
    //        /// </summary>
    //        /// <returns>True if a new permutation has been returned, false if not.</returns>
    //        /// <remarks>
    //        /// This uses the integers of the lexicographical order of the values so that any
    //        /// comparison of values are only performed during initialization. 
    //        /// </remarks>
    //        private bool NextPermutation()
    //        {
    //            int i = myLexicographicalOrders.Length - 1;
    //            while (myLexicographicalOrders[i - 1] >= myLexicographicalOrders[i])
    //            {
    //                --i;
    //                if (i == 0)
    //                {
    //                    return false;
    //                }
    //            }
    //            int j = myLexicographicalOrders.Length;
    //            while (myLexicographicalOrders[j - 1] <= myLexicographicalOrders[i - 1])
    //            {
    //                --j;
    //            }
    //            Swap(i - 1, j - 1);
    //            ++i;
    //            j = myLexicographicalOrders.Length;
    //            while (i < j)
    //            {
    //                Swap(i - 1, j - 1);
    //                ++i;
    //                --j;
    //            }
    //            return true;
    //        }

    //        /// <summary>
    //        /// Helper function for swapping two elements within the internal collection.
    //        /// This swaps both the lexicographical order and the values, maintaining the parallel array.
    //        /// </summary>
    //        private void Swap(int i, int j)
    //        {
    //            myTemp = myValues[i];
    //            myValues[i] = myValues[j];
    //            myValues[j] = myTemp;
    //            myKviTemp = myLexicographicalOrders[i];
    //            myLexicographicalOrders[i] = myLexicographicalOrders[j];
    //            myLexicographicalOrders[j] = myKviTemp;
    //        }

    //        #endregion

    //        #region Data and Internal Members
    //        /// <summary>
    //        /// Single instance of swap variable for T, small performance improvement over declaring in Swap function scope.
    //        /// </summary>
    //        private T myTemp;

    //        /// <summary>
    //        /// Single instance of swap variable for int, small performance improvement over declaring in Swap function scope.
    //        /// </summary>
    //        private int myKviTemp;

    //        /// <summary>
    //        /// Flag indicating the position of the enumerator.
    //        /// </summary>
    //        private Position myPosition = Position.BeforeFirst;

    //        /// <summary>
    //        /// Parrellel array of integers that represent the location of items in the myValues array.
    //        /// This is generated at Initialization and is used as a performance speed up rather that
    //        /// comparing T each time, much faster to let the CLR optimize around integers.
    //        /// </summary>
    //        private int[] myLexicographicalOrders;

    //        /// <summary>
    //        /// The list of values that are current to the enumerator.
    //        /// </summary>
    //        private List<T> myValues;

    //        /// <summary>
    //        /// The set of permuations that this enumerator enumerates.
    //        /// </summary>
    //        private Permutations<T> myParent;

    //        /// <summary>
    //        /// Internal position type for tracking enumertor position.
    //        /// </summary>
    //        private enum Position
    //        {
    //            BeforeFirst,
    //            InSet,
    //            AfterLast
    //        }

    //        #endregion

    //    }

    //    #endregion

    //    #region IMetaList Interface

    //    /// <summary>
    //    /// The count of all permutations that will be returned.
    //    /// If type is MetaCollectionType.WithholdGeneratedSets, then this does not double count permutations with multiple identical values.  
    //    /// I.e. count of permutations of "AAB" will be 3 instead of 6.  
    //    /// If type is MetaCollectionType.WithRepetition, then this is all combinations and is therefore N!, where N is the number of values.
    //    /// </summary>
    //    public long Count
    //    {
    //        get
    //        {
    //            return myCount;
    //        }
    //    }

    //    /// <summary>
    //    /// The type of Permutations set that is generated.
    //    /// </summary>
    //    public GenerateOption Type
    //    {
    //        get
    //        {
    //            return myMetaCollectionType;
    //        }
    //    }

    //    /// <summary>
    //    /// The upper index of the meta-collection, equal to the number of items in the initial set.
    //    /// </summary>
    //    public int UpperIndex
    //    {
    //        get
    //        {
    //            return myValues.Count;
    //        }
    //    }

    //    /// <summary>
    //    /// The lower index of the meta-collection, equal to the number of items returned each iteration.
    //    /// For Permutation, this is always equal to the UpperIndex.
    //    /// </summary>
    //    public int LowerIndex
    //    {
    //        get
    //        {
    //            return myValues.Count;
    //        }
    //    }

    //    #endregion

    //    #region Heavy Lifting Members

    //    /// <summary>
    //    /// Common intializer used by the multiple flavors of constructors.
    //    /// </summary>
    //    /// <remarks>
    //    /// Copies information provided and then creates a parellel int array of lexicographic
    //    /// orders that will be used for the actual permutation algorithm.  
    //    /// The input array is first sorted as required for WithoutRepetition and always just for consistency.
    //    /// This array is constructed one of two way depending on the type of the collection.
    //    ///
    //    /// When type is MetaCollectionType.WithRepetition, then all N! permutations are returned
    //    /// and the lexicographic orders are simply generated as 1, 2, ... N.  
    //    /// E.g.
    //    /// Input array:          {A A B C D E E}
    //    /// Lexicograhpic Orders: {1 2 3 4 5 6 7}
    //    /// 
    //    /// When type is MetaCollectionType.WithoutRepetition, then fewer are generated, with each
    //    /// identical element in the input array not repeated.  The lexicographic sort algorithm
    //    /// handles this natively as long as the repetition is repeated.
    //    /// E.g.
    //    /// Input array:          {A A B C D E E}
    //    /// Lexicograhpic Orders: {1 1 2 3 4 5 5}
    //    /// </remarks>
    //    private void Initialize(IList<T> values, GenerateOption type, IComparer<T> comparer)
    //    {
    //        myMetaCollectionType = type;
    //        myValues = new List<T>(values.Count);
    //        myValues.AddRange(values);
    //        myLexicographicOrders = new int[values.Count];
    //        if (type == GenerateOption.WithRepetition)
    //        {
    //            for (int i = 0; i < myLexicographicOrders.Length; ++i)
    //            {
    //                myLexicographicOrders[i] = i;
    //            }
    //        }
    //        else
    //        {
    //            if (comparer == null)
    //            {
    //                comparer = new SelfComparer<T>();
    //            }
    //            myValues.Sort(comparer);
    //            int j = 1;
    //            if (myLexicographicOrders.Length > 0)
    //            {
    //                myLexicographicOrders[0] = j;
    //            }
    //            for (int i = 1; i < myLexicographicOrders.Length; ++i)
    //            {
    //                if (comparer.Compare(myValues[i - 1], myValues[i]) != 0)
    //                {
    //                    ++j;
    //                }
    //                myLexicographicOrders[i] = j;
    //            }
    //        }
    //        myCount = GetCount();
    //    }

    //    /// <summary>
    //    /// Calculates the total number of permutations that will be returned.  
    //    /// As this can grow very large, extra effort is taken to avoid overflowing the accumulator.  
    //    /// While the algorithm looks complex, it really is just collecting numerator and denominator terms
    //    /// and cancelling out all of the denominator terms before taking the product of the numerator terms.  
    //    /// </summary>
    //    /// <returns>The number of permutations.</returns>
    //    private long GetCount()
    //    {
    //        int runCount = 1;
    //        List<int> divisors = new List<int>();
    //        List<int> numerators = new List<int>();
    //        for (int i = 1; i < myLexicographicOrders.Length; ++i)
    //        {
    //            numerators.AddRange(SmallPrimeUtility.Factor(i + 1));
    //            if (myLexicographicOrders[i] == myLexicographicOrders[i - 1])
    //            {
    //                ++runCount;
    //            }
    //            else
    //            {
    //                for (int f = 2; f <= runCount; ++f)
    //                {
    //                    divisors.AddRange(SmallPrimeUtility.Factor(f));
    //                }
    //                runCount = 1;
    //            }
    //        }
    //        for (int f = 2; f <= runCount; ++f)
    //        {
    //            divisors.AddRange(SmallPrimeUtility.Factor(f));
    //        }
    //        return SmallPrimeUtility.EvaluatePrimeFactors(SmallPrimeUtility.DividePrimeFactors(numerators, divisors));
    //    }

    //    #endregion

    //    #region Data and Internal Members

    //    /// <summary>
    //    /// A list of T that represents the order of elements as originally provided, used for Reset.
    //    /// </summary>
    //    private List<T> myValues;

    //    /// <summary>
    //    /// Parrellel array of integers that represent the location of items in the myValues array.
    //    /// This is generated at Initialization and is used as a performance speed up rather that
    //    /// comparing T each time, much faster to let the CLR optimize around integers.
    //    /// </summary>
    //    private int[] myLexicographicOrders;

    //    /// <summary>
    //    /// Inner class that wraps an IComparer around a type T when it is IComparable
    //    /// </summary>
    //    private class SelfComparer<U> : IComparer<U>
    //    {
    //        public int Compare(U x, U y)
    //        {
    //            return ((IComparable<U>)x).CompareTo(y);
    //        }
    //    }

    //    /// <summary>
    //    /// The count of all permutations.  Calculated at Initialization and returned by Count property.
    //    /// </summary>
    //    private long myCount;

    //    /// <summary>
    //    /// The type of Permutations that this was intialized from.
    //    /// </summary>
    //    private GenerateOption myMetaCollectionType;

    //    #endregion

    //}

    //public class Combinations<T> : IMetaCollection<T>
    //{
    //    #region Constructors

    //    /// <summary>
    //    /// No default constructor, must provided a list of values and size.
    //    /// </summary>
    //    protected Combinations()
    //    {
    //        ;
    //    }

    //    /// <summary>
    //    /// Create a combination set from the provided list of values.
    //    /// The upper index is calculated as values.Count, the lower index is specified.
    //    /// Collection type defaults to MetaCollectionType.WithoutRepetition
    //    /// </summary>
    //    /// <param name="values">List of values to select combinations from.</param>
    //    /// <param name="lowerIndex">The size of each combination set to return.</param>
    //    public Combinations(IList<T> values, int lowerIndex)
    //    {
    //        Initialize(values, lowerIndex, GenerateOption.WithoutRepetition);
    //    }

    //    /// <summary>
    //    /// Create a combination set from the provided list of values.
    //    /// The upper index is calculated as values.Count, the lower index is specified.
    //    /// </summary>
    //    /// <param name="values">List of values to select combinations from.</param>
    //    /// <param name="lowerIndex">The size of each combination set to return.</param>
    //    /// <param name="type">The type of Combinations set to generate.</param>
    //    public Combinations(IList<T> values, int lowerIndex, GenerateOption type)
    //    {
    //        Initialize(values, lowerIndex, type);
    //    }

    //    #endregion

    //    #region IEnumerable Interface

    //    /// <summary>
    //    /// Gets an enumerator for collecting the list of combinations.
    //    /// </summary>
    //    /// <returns>The enumerator.</returns>
    //    public IEnumerator<IList<T>> GetEnumerator()
    //    {
    //        return new Enumerator(this);
    //    }

    //    /// <summary>
    //    /// Gets an enumerator for collecting the list of combinations.
    //    /// </summary>
    //    /// <returns>The enumerator.returns>
    //    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    //    {
    //        return new Enumerator(this);
    //    }

    //    #endregion

    //    #region Enumerator Inner Class

    //    /// <summary>
    //    /// The enumerator that enumerates each meta-collection of the enclosing Combinations class.
    //    /// </summary>
    //    public class Enumerator : IEnumerator<IList<T>>
    //    {

    //        #region Constructors

    //        /// <summary>
    //        /// Construct a enumerator with the parent object.
    //        /// </summary>
    //        /// <param name="source">The source combinations object.</param>
    //        public Enumerator(Combinations<T> source)
    //        {
    //            myParent = source;
    //            myPermutationsEnumerator = (Permutations<bool>.Enumerator)myParent.myPermutations.GetEnumerator();
    //        }

    //        #endregion

    //        #region IEnumerator interface
    //        /// <summary>
    //        /// Resets the combinations enumerator to the first combination.  
    //        /// </summary>
    //        public void Reset()
    //        {
    //            myPermutationsEnumerator.Reset();
    //        }

    //        /// <summary>
    //        /// Advances to the next combination of items from the set.
    //        /// </summary>
    //        /// <returns>True if successfully moved to next combination, False if no more unique combinations exist.</returns>
    //        /// <remarks>
    //        /// The heavy lifting is done by the permutations object, the combination is generated
    //        /// by creating a new list of those items that have a true in the permutation parrellel array.
    //        /// </remarks>
    //        public bool MoveNext()
    //        {
    //            bool ret = myPermutationsEnumerator.MoveNext();
    //            myCurrentList = null;
    //            return ret;
    //        }

    //        /// <summary>
    //        /// The current combination
    //        /// </summary>
    //        public IList<T> Current
    //        {
    //            get
    //            {
    //                ComputeCurrent();
    //                return myCurrentList;
    //            }
    //        }

    //        /// <summary>
    //        /// The current combination
    //        /// </summary>
    //        object System.Collections.IEnumerator.Current
    //        {
    //            get
    //            {
    //                ComputeCurrent();
    //                return myCurrentList;
    //            }
    //        }

    //        /// <summary>
    //        /// Cleans up non-managed resources, of which there are none used here.
    //        /// </summary>
    //        public void Dispose()
    //        {
    //            ;
    //        }

    //        #endregion

    //        #region Heavy Lifting Members

    //        /// <summary>
    //        /// The only complex function of this entire wrapper, ComputeCurrent() creates
    //        /// a list of original values from the bool permutation provided.  
    //        /// The exception for accessing current (InvalidOperationException) is generated
    //        /// by the call to .Current on the underlying enumeration.
    //        /// </summary>
    //        /// <remarks>
    //        /// To compute the current list of values, the underlying permutation object
    //        /// which moves with this enumerator, is scanned differently based on the type.
    //        /// The items have only two values, true and false, which have different meanings:
    //        /// 
    //        /// For type WithoutRepetition, the output is a straightforward subset of the input array.  
    //        /// E.g. 6 choose 3 without repetition
    //        /// Input array:   {A B C D E F}
    //        /// Permutations:  {0 1 0 0 1 1}
    //        /// Generates set: {A   C D    }
    //        /// Note: size of permutation is equal to upper index.
    //        /// 
    //        /// For type WithRepetition, the output is defined by runs of characters and when to 
    //        /// move to the next element.
    //        /// E.g. 6 choose 5 with repetition
    //        /// Input array:   {A B C D E F}
    //        /// Permutations:  {0 1 0 0 1 1 0 0 1 1}
    //        /// Generates set: {A   B B     D D    }
    //        /// Note: size of permutation is equal to upper index - 1 + lower index.
    //        /// </remarks>
    //        private void ComputeCurrent()
    //        {
    //            if (myCurrentList == null)
    //            {
    //                myCurrentList = new List<T>();
    //                int index = 0;
    //                IList<bool> currentPermutation = (IList<bool>)myPermutationsEnumerator.Current;
    //                for (int i = 0; i < currentPermutation.Count; ++i)
    //                {
    //                    if (currentPermutation[i] == false)
    //                    {
    //                        myCurrentList.Add(myParent.myValues[index]);
    //                        if (myParent.Type == GenerateOption.WithoutRepetition)
    //                        {
    //                            ++index;
    //                        }
    //                    }
    //                    else
    //                    {
    //                        ++index;
    //                    }
    //                }
    //            }
    //        }

    //        #endregion

    //        #region Data

    //        /// <summary>
    //        /// Parent object this is an enumerator for.
    //        /// </summary>
    //        private Combinations<T> myParent;

    //        /// <summary>
    //        /// The current list of values, this is lazy evaluated by the Current property.
    //        /// </summary>
    //        private List<T> myCurrentList;

    //        /// <summary>
    //        /// An enumertor of the parents list of lexicographic orderings.
    //        /// </summary>
    //        private Permutations<bool>.Enumerator myPermutationsEnumerator;

    //        #endregion
    //    }
    //    #endregion

    //    #region IMetaList Interface

    //    /// <summary>
    //    /// The number of unique combinations that are defined in this meta-collection.
    //    /// This value is mathematically defined as Choose(M, N) where M is the set size
    //    /// and N is the subset size.  This is M! / (N! * (M-N)!).
    //    /// </summary>
    //    public long Count
    //    {
    //        get
    //        {
    //            return myPermutations.Count;
    //        }
    //    }

    //    /// <summary>
    //    /// The type of Combinations set that is generated.
    //    /// </summary>
    //    public GenerateOption Type
    //    {
    //        get
    //        {
    //            return myMetaCollectionType;
    //        }
    //    }

    //    /// <summary>
    //    /// The upper index of the meta-collection, equal to the number of items in the initial set.
    //    /// </summary>
    //    public int UpperIndex
    //    {
    //        get
    //        {
    //            return myValues.Count;
    //        }
    //    }

    //    /// <summary>
    //    /// The lower index of the meta-collection, equal to the number of items returned each iteration.
    //    /// </summary>
    //    public int LowerIndex
    //    {
    //        get
    //        {
    //            return myLowerIndex;
    //        }
    //    }

    //    #endregion

    //    #region Heavy Lifting Members

    //    /// <summary>
    //    /// Initialize the combinations by settings a copy of the values from the 
    //    /// </summary>
    //    /// <param name="values">List of values to select combinations from.</param>
    //    /// <param name="lowerIndex">The size of each combination set to return.</param>
    //    /// <param name="type">The type of Combinations set to generate.</param>
    //    /// <remarks>
    //    /// Copies the array and parameters and then creates a map of booleans that will 
    //    /// be used by a permutations object to refence the subset.  This map is slightly
    //    /// different based on whether the type is with or without repetition.
    //    /// 
    //    /// When the type is WithoutRepetition, then a map of upper index elements is
    //    /// created with lower index false's.  
    //    /// E.g. 8 choose 3 generates:
    //    /// Map: {1 1 1 1 1 0 0 0}
    //    /// Note: For sorting reasons, false denotes inclusion in output.
    //    /// 
    //    /// When the type is WithRepetition, then a map of upper index - 1 + lower index
    //    /// elements is created with the falses indicating that the 'current' element should
    //    /// be included and the trues meaning to advance the 'current' element by one.
    //    /// E.g. 8 choose 3 generates:
    //    /// Map: {1 1 1 1 1 1 1 1 0 0 0} (7 trues, 3 falses).
    //    /// </remarks>
    //    private void Initialize(IList<T> values, int lowerIndex, GenerateOption type)
    //    {
    //        myMetaCollectionType = type;
    //        myLowerIndex = lowerIndex;
    //        myValues = new List<T>();
    //        myValues.AddRange(values);
    //        List<bool> myMap = new List<bool>();
    //        if (type == GenerateOption.WithoutRepetition)
    //        {
    //            for (int i = 0; i < myValues.Count; ++i)
    //            {
    //                if (i >= myValues.Count - myLowerIndex)
    //                {
    //                    myMap.Add(false);
    //                }
    //                else
    //                {
    //                    myMap.Add(true);
    //                }
    //            }
    //        }
    //        else
    //        {
    //            for (int i = 0; i < values.Count - 1; ++i)
    //            {
    //                myMap.Add(true);
    //            }
    //            for (int i = 0; i < myLowerIndex; ++i)
    //            {
    //                myMap.Add(false);
    //            }
    //        }
    //        myPermutations = new Permutations<bool>(myMap);
    //    }

    //    #endregion

    //    #region Data

    //    /// <summary>
    //    /// Copy of values object is intialized with, required for enumerator reset.
    //    /// </summary>
    //    private List<T> myValues;

    //    /// <summary>
    //    /// Permutations object that handles permutations on booleans for combination inclusion.
    //    /// </summary>
    //    private Permutations<bool> myPermutations;

    //    /// <summary>
    //    /// The type of the combination collection.
    //    /// </summary>
    //    private GenerateOption myMetaCollectionType;

    //    /// <summary>
    //    /// The lower index defined in the constructor.
    //    /// </summary>
    //    private int myLowerIndex;

    //    #endregion
    //}
}
