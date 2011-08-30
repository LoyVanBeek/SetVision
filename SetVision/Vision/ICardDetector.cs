using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SetVision.Gamelogic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace SetVision.Vision
{
    public interface ICardDetector
    {
        IDictionary<Card, Point> LocateCards(Image<Bgr, Byte> table);
    }
}
