using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SetVision.Gamelogic;
using Emgu.CV.Structure;

namespace SetVision.Learning
{
    public class BgrClassifier
    {
        private enum VisionColor
        {
            White,
            Black,
            Blue,
            Brown,
            Gray,
            Green,
            Orange,
            Pink,
            Red,
            Yellow,
            Purple
        }

        KNearestClassifier kmeans;

        public BgrClassifier()
        {
            kmeans = new KNearestClassifier();

            Dictionary<float[], string> data = new Dictionary<float[], string>();
            foreach (KeyValuePair<float[], string> item in TrainCsv())
            {
                data.Add(item.Key, item.Value);
            }
            kmeans.Train(data);
        }

        #region train on dirs
        private IEnumerable<KeyValuePair<float[], int>> TrainDirectories()
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
                        float[] array = new float[] { (float)bgr.Blue, (float)bgr.Green, (float)bgr.Red };
                        KeyValuePair<float[], int> pair = new KeyValuePair<float[], int>(array, (int)color);
                        yield return pair;
                    }
                }
            }
        }

        private IEnumerable<KeyValuePair<float[], string>> TrainDirectories_String()
        {
            DirectoryInfo colordebug = new DirectoryInfo(@"D:\Development\OpenCV\SetVision\SetVision\bin\Debug\colordebug");
            DirectoryInfo pass4 = colordebug.GetDirectories("Pass 9")[0];

            foreach (DirectoryInfo colordir in pass4.GetDirectories())
            {
                string colname = colordir.Name.ToLower();

                foreach (FileInfo file in colordir.GetFiles())
                {
                    Bgr bgr; Hsv hsv;
                    fileNameToColors(file.Name, out bgr, out hsv);
                    if (!bgr.Equals(new Bgr()) && !hsv.Equals(new Hsv()))
                    {
                        float[] array = new float[] { (float)bgr.Blue, (float)bgr.Green, (float)bgr.Red };
                        KeyValuePair<float[], string> pair = 
                            new KeyValuePair<float[], string>(array, colname);
                        yield return pair;
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
        #endregion

        private IEnumerable<KeyValuePair<float[], string>> TrainCsv()
        {
            StreamReader reader = new StreamReader(@"D:\Development\OpenCV\SetVision\recordings.csv");
            string line = reader.ReadLine();
            do
            {
                line = reader.ReadLine();
                if (!String.IsNullOrEmpty(line.Replace(';',' ')))
                {
                    string[] parts = line.Split(';');
                    float b = float.Parse(parts[0]);
                    float g = float.Parse(parts[1]);
                    float r = float.Parse(parts[2]);

                    string color = parts[4].ToLower(); //4rd is empty, so take 5th column

                    yield return new KeyValuePair<float[], string>(new float[] { r, g, b }, color); 
                }
            } 
            while (reader.Peek() != -1);
        }

        //public CardColor Classify(Bgr value)
        //{
        //    float[] array = new float[] { (float)value.Blue, (float)value.Green, (float)value.Red };
        //    //CardColor outcome = (CardColor)kmeans.Classify(array);
        //    string colname = kmeans.ClassifyToString(array);

        //    CardColor color = CardColor.Other;
        //    if (colname.Contains("purple"))
        //    {
        //        color = CardColor.Purple;
        //    }
        //    else if (colname.Contains("green"))
        //    {
        //        color = CardColor.Green;
        //    }
        //    else if (colname.Contains("red") || colname.Contains("pink"))
        //    {
        //        color = CardColor.Red;
        //    }
        //    else if (colname.Contains("white"))
        //    {
        //        color = CardColor.White;
        //    }

        //    return color;
        //}

        private VisionColor Classify_Vision(Bgr value)
        {
            float[] array = new float[] { (float)value.Blue, (float)value.Green, (float)value.Red };
            //CardColor outcome = (CardColor)kmeans.Classify(array);
            string colname = kmeans.ClassifyToString(array);

            VisionColor color = (VisionColor)Enum.Parse(typeof(VisionColor), colname, true);

            return color;
        }

        public CardColor Classify(Bgr value)
        {
            VisionColor vis = Classify_Vision(value);
            switch (vis)
            {
                case VisionColor.Black: return CardColor.Other;
                case VisionColor.Blue: return CardColor.Other;
                case VisionColor.Brown: return CardColor.Other;
                case VisionColor.Gray: return CardColor.White;
                case VisionColor.Green: return CardColor.Green;
                case VisionColor.Orange: return CardColor.Red;
                case VisionColor.Pink: return CardColor.Red;
                case VisionColor.Red: return CardColor.Red;
                case VisionColor.White: return CardColor.White;
                case VisionColor.Yellow: return CardColor.Other;
                case VisionColor.Purple: return CardColor.Purple;
            }
            return CardColor.Other;
        }

        public static void Test()
        {
            BgrClassifier clas = new BgrClassifier();
            CardColor c1 = clas.Classify(new Bgr(145, 110, 197));  //Purple
            CardColor c2 = clas.Classify(new Bgr(255, 255, 255));  //White?
            CardColor c3 = clas.Classify(new Bgr(0, 0, 250));  //Red
            CardColor c4 = clas.Classify(new Bgr(53, 0, 250));  //Red
            CardColor c5 = clas.Classify(new Bgr(250, 0, 250));  //Purple
            CardColor c6 = clas.Classify(new Bgr(250, 0, 251));  //Purple
            CardColor c7 = clas.Classify(new Bgr(240, 0, 240));  //Purple
            CardColor c8 = clas.Classify(new Bgr(145, 110, 194));  //Purple
            
            bool ok1 = CardColor.Purple    == c1;  //Purple
            bool ok2 = CardColor.White     == c2;  //White?
            bool ok3 = CardColor.Red       == c3;  //Red
            bool ok4 = CardColor.Red       == c4;  //Red
            bool ok5 = CardColor.Purple    == c5;  //Purple
            bool ok6 = CardColor.Purple    == c6;  //Purple
            bool ok7 = CardColor.Purple    == c7;  //Purple
            bool ok8 = CardColor.Purple    == c8;  //Purple

        }
    }
}
