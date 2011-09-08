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

        public void Train(IDictionary<float[], int> trainpairs)
        {
            Matrix<float> vectors;
            Matrix<float> classes;
            ClassifierUtils.GenerateTrainMatrices(trainpairs, out vectors, out classes);

            classifier = new KNearest(vectors, classes, null, false, 10); //TODO: set these parameters in Constructor
        }
        #endregion
    }
}
