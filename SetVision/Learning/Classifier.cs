using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using System.IO;

namespace SetVision.Learning
{
    public abstract class Classifier
    {
        protected Dictionary<int, string> LUT;
        protected Dictionary<string, int> TUL; //inverse of Lookup table

        public static void GenerateTrainMatrices(IDictionary<float[], int> trainpairs,
            out Matrix<float> vectors,
            out Matrix<float> classes)
        {
            List<float[]> samples = new List<float[]>(trainpairs.Keys);

            int dimension = samples[0].Length;
            vectors = new Matrix<float>(trainpairs.Count, dimension);
            classes = new Matrix<float>(trainpairs.Count, 1);

            for (int sample_index = 0; sample_index < trainpairs.Count; sample_index++)
            {
                float[] sample = samples[sample_index];
                for (int i = 0; i < dimension; i++)
                {
                    vectors[sample_index, i] = sample[i];
                }

                classes[sample_index, 0] = (float)trainpairs[sample];
            }
        }

        public static Matrix<float> ToMatrix(float[] vector)
        {
            Matrix<float> toClassify = new Matrix<float>(1, vector.Length);
            for (int i = 0; i < vector.Length; i++)
            {
                toClassify[0, i] = vector[i];
            }
            return toClassify;
        }

        public virtual int Classify(float[] vector)
        {
            throw new NotImplementedException();
        }

        public string ClassifyToString(float[] vector)
        {
            int key = this.Classify(vector);
            return this.LUT[key];
        }

        public virtual void Train(IDictionary<float[], int> trainpairs)
        {
            throw new NotImplementedException();
        }

        public virtual void Train(IDictionary<float[], string> trainpairs)
        {
            throw new NotImplementedException();
        }

        protected void InitLookups(IDictionary<float[], string> trainpairs)
        {
            LUT = new Dictionary<int, string>(trainpairs.Count); //LookUp Table
            TUL = new Dictionary<string, int>(trainpairs.Count); //reverse LookUp Table

            HashSet<string> strings = new HashSet<string>(trainpairs.Values);
            string[] keys = strings.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                this.LUT.Add(i, keys[i]);
                try
                {
                    this.TUL.Add(keys[i], i);
                }
                catch (ArgumentException) { }
            }
        }

        protected IEnumerable<KeyValuePair<float[], string>> TrainCsv(string filename)
        {
            StreamReader reader = new StreamReader(filename);
            string line = reader.ReadLine();
            do
            {
                line = reader.ReadLine();
                if (!String.IsNullOrEmpty(line.Replace(';', ' ')))
                {
                    //string[] parts = line.Split(';');
                    List<string> parts = new List<string>(line.Split(';'));
                    List<float> fields = new List<float>(parts.Count-1); //last field is string
                    string label = "";
                    foreach (string part in parts)
                    {
                        if (!String.IsNullOrEmpty(part))
                        {
                            try
                            {
                                float flt = float.Parse(part);
                                fields.Add(flt);
                            }
                            catch (FormatException)
                            {
                                label = part;
                                break;
                            } 
                        }
                    }

                    yield return new KeyValuePair<float[], string>(fields.ToArray(), label);
                }
            }
            while (reader.Peek() != -1);
        }

        public void TrainFromCsv(string filename)
        {
            Dictionary<float[], string> data = new Dictionary<float[], string>();
            foreach (KeyValuePair<float[], string> item in TrainCsv(filename))
            {
                data.Add(item.Key, item.Value);
            }
            this.Train(data);
        }
    }
}
