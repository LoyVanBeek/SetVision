using System;
using Emgu.CV.ML;
using System.Collections.Generic;
namespace SetVision.Learning
{
    interface IClassifier
    {
        int Classify(float[] vector);
        void Train(IDictionary<float[], int> trainpairs);
    }
}
