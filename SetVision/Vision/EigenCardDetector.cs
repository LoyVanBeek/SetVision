using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using System.IO;
using Emgu.CV.Structure;
using System.Drawing;

namespace SetVision.Vision
{
    class EigenCardDetector : ICardDetector
    {
        EigenObjectRecognizer recog;

        public EigenCardDetector(string foldername)
        {
            List<FileInfo> files = new List<FileInfo>(new DirectoryInfo(foldername).GetFiles());
            List<Image<Gray, byte>> images = new List<Image<Gray, byte>>(files.Count);
            foreach (FileInfo info in files)
            {
                Bitmap bit = new Bitmap(info.FullName);
                images.Add(new Image<Gray, byte>(bit));
            }

            MCvTermCriteria crit = new MCvTermCriteria(0.05);
            recog = new EigenObjectRecognizer(images.ToArray(), ref crit);
        }

        #region ICardDetector Members

        public Dictionary<SetVision.Gamelogic.Card, Point> LocateCards(Image<Bgr, byte> table)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
