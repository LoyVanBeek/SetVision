using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV.ML;
using Emgu.CV;
using Emgu.CV.Structure;

namespace SetVision.Learning
{
    public class SVMClassifier : Classifier
    {
        SVM model;
        public SVMParams parameters;
        
        public SVMClassifier()//SVMParams _parameters
        {
            model = new SVM();
            
            SVMParams p = new SVMParams();
            p.KernelType = Emgu.CV.ML.MlEnum.SVM_KERNEL_TYPE.LINEAR;
            p.SVMType = Emgu.CV.ML.MlEnum.SVM_TYPE.C_SVC;
            p.C = 1;
            p.TermCrit = new MCvTermCriteria(100, 0.00001);

            parameters = p;
        }

        public SVMClassifier(string filename)//SVMParams _parameters
        {
            model = new SVM();
            model.Load(filename);
        }

        public override void Train(IDictionary<float[], int> trainpairs)
        {
            Matrix<float> vectors;
            Matrix<float> classes;
            ClassifierUtils.GenerateTrainMatrices(trainpairs, out vectors, out classes);

            model.TrainAuto(vectors, classes, null, null, this.parameters.MCvSVMParams, 5);
        }

        public override void Train(IDictionary<float[], string> trainpairs)
        {
            Matrix<float> vectors;
            Matrix<float> classes;
            Dictionary<float[], int> trainers = new Dictionary<float[], int>(trainpairs.Count);

            InitLookups(trainpairs);

            foreach (KeyValuePair<float[], string> row in trainpairs)
            {
                trainers.Add(row.Key, TUL[row.Value]);
            }

            GenerateTrainMatrices(trainers, out vectors, out classes);

            model.TrainAuto(vectors, classes, null, null, this.parameters.MCvSVMParams, 5);
        }

        public override int Classify(float[] vector)
        {
            Matrix<float> toClassify = ClassifierUtils.ToMatrix(vector);
            
            float response = model.Predict(toClassify);

            return (int)response;
        }

        public void Save(string filename)
        {
            model.Save(filename);
        }
    }
}
