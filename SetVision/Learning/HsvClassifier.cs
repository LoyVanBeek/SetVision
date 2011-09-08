using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SetVision.Gamelogic;
using Emgu.CV.Structure;

namespace SetVision.Learning
{
    public class HsvClassifier
    {
        KmeansClassifier kmeans;

        public HsvClassifier()
        {
            kmeans = new KmeansClassifier();

            Dictionary<float[], int> data = new Dictionary<float[], int>();
            foreach (KeyValuePair<float[], int> item in GenerateTrainPairs())
            {
                data.Add(item.Key, item.Value);
            }
            kmeans.Train(data);
        }

        private IEnumerable<KeyValuePair<float[], int>> GenerateTrainPairs()
        {
            DirectoryInfo colordebug = new DirectoryInfo(@"D:\Development\OpenCV\SetVision\SetVision\bin\Debug\colordebug");
            DirectoryInfo pass4 = colordebug.GetDirectories("Pass 4")[0];

            foreach (DirectoryInfo colordir in pass4.GetDirectories())
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
                        float[] array = new float[] { (float)hsv.Hue, (float)hsv.Satuation, (float)hsv.Value };
                        yield return new KeyValuePair<float[], int>(array, (int)color);
                    }
                }
            }
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

        public CardColor Classify(Hsv hsv)
        {
            float[] array = new float[] { (float)hsv.Hue, (float)hsv.Satuation, (float)hsv.Value };
            CardColor outcome = (CardColor)kmeans.Classify(array);
            return outcome;
        }
    }
}
