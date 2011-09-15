using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SetVision.Gamelogic;
using Emgu.CV.Structure;
using Emgu.CV;
using Emgu.CV.ML;

namespace SetVision.Learning
{
    public class BgrHsvClassifier
    {
        private struct ColorPair
        {
            public Bgr Bgr;
            public Hsv Hsv;

            public ColorPair(Bgr a, Hsv b)
            {
                Bgr = a;
                Hsv = b;
            }
        }

        private KNearest bgrClassifier;
        private KNearest hsvClassifier;

        private IEnumerable<KeyValuePair<ColorPair, CardColor>> GenerateTrainPairs()
        {
            CsvWriter writer = new CsvWriter(@"D:\Development\OpenCV\SetVision\SetVision\bin\Debug\colordebug\generated.csv");

            DirectoryInfo colordebug = new DirectoryInfo(@"D:\Development\OpenCV\SetVision\SetVision\bin\Debug\colordebug");
            DirectoryInfo traindir = colordebug.GetDirectories("Pass 9")[0];

            foreach (DirectoryInfo colordir in traindir.GetDirectories())
            {
                CardColor color = CardColor.Other;
                string colname = colordir.Name.ToLower();
                if (colname.Contains("purple"))
                {
                    color = CardColor.Purple;
                }
                else if (colname.Contains("green"))
                {
                    color = CardColor.Green;
                }
                else if (colname.Contains("red"))
                {
                    color = CardColor.Red;
                }
                else if (colname.Contains("white"))
                {
                    color = CardColor.White;
                }

                foreach (FileInfo file in colordir.GetFiles())
                {
                    Bgr bgr; Hsv hsv;
                    fileNameToColors(file.Name, out bgr, out hsv);
                    if (!bgr.Equals(new Bgr()) && !hsv.Equals(new Hsv()))
                    {
                        ColorPair colors = new ColorPair(bgr, hsv);
                        writer.Write((int)bgr.Blue, (int)bgr.Green, (int)bgr.Red);

                        yield return new KeyValuePair<ColorPair, CardColor>(colors, color);
                    }
                }
            }
            writer.Close();
        }

        private void fileNameToColors(string filename, out Bgr bgr, out Hsv hsv)
        {
            //This is a filename:
            //B146, G159, R136=Green - H123, S80, V178=Purple VERDICT=Purple.png
            string[] parts = filename.Split(' ', '=');//, 'B', 'G', 'R', 'H', 'S', 'V'
            /*
             * With this splitting, 
             * parts[0] = B-value, 
             * parts[1] = G-value, 
             * parts[2] = R-value,
             *  
             * parts[3] = H-value, 
             * parts[4] = S-value, 
             * parts[5] = V-value,
             */

            try
            {
                string B = parts[0].Replace(',', ' ').Substring(1);
                string G = parts[1].Replace(',', ' ').Substring(1);
                string R = parts[2].Replace(',', ' ').Substring(1);
                string H = parts[5].Replace(',', ' ').Substring(1);
                string S = parts[6].Replace(',', ' ').Substring(1);
                string V = parts[7].Replace(',', ' ').Substring(1);

                int b = int.Parse(B);
                int g = int.Parse(G);
                int r = int.Parse(R);
                int h = int.Parse(H);
                int s = int.Parse(S);
                int v = int.Parse(V);

                bgr = new Bgr(b, g, r);
                hsv = new Hsv(h, s, v);
            }
            catch (Exception)
            {
                bgr = new Bgr();
                hsv = new Hsv();
            }

        }

        public void Train()
        {
            /*
             * in trainData:    data[i,.,.,.]   = vector
             * trainClasses: classes[i]         = class
             */
            List<KeyValuePair<ColorPair, CardColor>> pairs = new List<KeyValuePair<ColorPair, CardColor>>(GenerateTrainPairs());

            #region Generate the traning data and classes
            Matrix<float> bgrTraining = new Matrix<float>(pairs.Count, 3);
            Matrix<float> hsvTraining = new Matrix<float>(pairs.Count, 3);
            Matrix<float> colorClasses = new Matrix<float>(pairs.Count, 1);

            for (int i = 0; i < pairs.Count; i++)
            {
                bgrTraining[i, 0] = (float)pairs[i].Key.Bgr.Blue;
                bgrTraining[i, 1] = (float)pairs[i].Key.Bgr.Green;
                bgrTraining[i, 2] = (float)pairs[i].Key.Bgr.Red;

                hsvTraining[i, 0] = (float)pairs[i].Key.Hsv.Hue;
                hsvTraining[i, 1] = (float)pairs[i].Key.Hsv.Satuation;
                hsvTraining[i, 2] = (float)pairs[i].Key.Hsv.Value;

                colorClasses[i, 0] = (float)(int)pairs[i].Value;
            }
            #endregion

            bgrClassifier = new KNearest(bgrTraining, colorClasses, null, false, 10);
            hsvClassifier = new KNearest(hsvTraining, colorClasses, null, false, 10);

            try
            {
                bgrClassifier.Save("bgr.txt");
                hsvClassifier.Save("hsv.txt");
            }
            catch (Exception)
            {
            }
        }

        public CardColor Classify(Bgr bgr)
        {
            Matrix<float> toClassify = new Matrix<float>(1, 3);
            toClassify[0, 0] = (float)bgr.Blue;
            toClassify[0, 1] = (float)bgr.Green;
            toClassify[0, 2] = (float)bgr.Red;

            Matrix<float> classification = new Matrix<float>(1, 1);
            bgrClassifier.FindNearest(toClassify, 
                5, 
                classification, 
                null, 
                null, 
                null);
            CardColor color = (CardColor)classification[0, 0];

            return color;
        }
        
        public CardColor Classify(Hsv hsv)
        {
            Matrix<float> toClassify = new Matrix<float>(1, 3);
            toClassify[0, 0] = (float)hsv.Hue;
            toClassify[0, 1] = (float)hsv.Satuation;
            toClassify[0, 2] = (float)hsv.Value;

            Matrix<float> classification = new Matrix<float>(1, 1);
            hsvClassifier.FindNearest(toClassify, 5, classification, null, null, null);
            CardColor color = (CardColor)classification[0, 0];

            return color;
        }

    }
}
