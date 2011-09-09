using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.ML;

namespace SetVision.Learning
{
    public class KmeansClassifier : IClassifier
    {
        #region IClassifier Members

        KNearest classifier;
        Dictionary<int, string> LUT;
        Dictionary<string, int> TUL; //inverse of Lookup table

        public int Classify(float[] vector)
        {
            Matrix<float> toClassify = ClassifierUtils.ToMatrix(vector);

            Matrix<float> classification = new Matrix<float>(1, 1);
            classifier.FindNearest(toClassify, 
                5, //TODO: Set all parameters in constructor
                classification, 
                null, 
                null, 
                null);

            return (int)classification[0,0];
        }

        public string ClassifyToString(float[] vector)
        {
            int key = Classify(vector);
            return LUT[key];
        }

        public void Train(IDictionary<float[], int> trainpairs)
        {
            Matrix<float> vectors;
            Matrix<float> classes;
            ClassifierUtils.GenerateTrainMatrices(trainpairs, out vectors, out classes);

            classifier = new KNearest(vectors, classes, null, false, 10); //TODO: set these parameters in Constructor
        }

        public void Train(IDictionary<float[], string> trainpairs)
        {
            Matrix<float> vectors;
            Matrix<float> classes;
            Dictionary<float[], int> trainers = new Dictionary<float[], int>(trainpairs.Count);

            InitLookups(trainpairs);

            foreach (KeyValuePair<float[], string> row in trainpairs)
            {
                trainers.Add(row.Key, TUL[row.Value]);
            }

            ClassifierUtils.GenerateTrainMatrices(trainers, out vectors, out classes);

            classifier = new KNearest(vectors, classes, null, false, 10); //TODO: set these parameters in Constructor
        }

        private void InitLookups(IDictionary<float[], string> trainpairs)
        {
            LUT = new Dictionary<int, string>(trainpairs.Count); //LookUp Table
            TUL = new Dictionary<string, int>(trainpairs.Count); //reverse LookUp Table

            HashSet<string> strings = new HashSet<string>(trainpairs.Values);
            string[] keys = strings.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                LUT.Add(i, keys[i]);
                try
                {
                    TUL.Add(keys[i], i);
                }
                catch (ArgumentException) { }
            }
        }
        #endregion

        private IDictionary<string, int> Invert(IDictionary<int, string> orig)
        {
            IDictionary<string, int> inverse = new Dictionary<string,int>();
            foreach(int key in orig.Keys)
            {
                string value = orig[key];
                try
                {
                    inverse.Add(value, key);
                }
                catch (ArgumentException)
                {
                }
            }
            return inverse;
        }
    }
}
