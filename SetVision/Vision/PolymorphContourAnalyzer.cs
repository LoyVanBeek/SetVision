using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SetVision.Gamelogic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

namespace SetVision.Vision
{
    public class PolymorphContourAnalyzer : ICardDetector
    {
        #region ICardDetector Members

        public Dictionary<Card, Point> LocateCards(Image<Bgr, Byte> table)
        {
            #region process image
            //Convert the image to grayscale and filter out the noise
            Image<Gray, Byte> gray = table.Convert<Gray, Byte>();

            Gray cannyThreshold = new Gray(180);
            Gray cannyThresholdLinking = new Gray(120);
            Gray circleAccumulatorThreshold = new Gray(120);

            Image<Gray, Byte> cannyEdges = gray.Canny(cannyThreshold, cannyThresholdLinking);

            StructuringElementEx el = new StructuringElementEx(3, 3, 1, 1, CV_ELEMENT_SHAPE.CV_SHAPE_RECT);
            cannyEdges = cannyEdges.MorphologyEx(el, CV_MORPH_OP.CV_MOP_CLOSE, 1);
            #endregion

            Contour<Point> contours = cannyEdges.FindContours(
                CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, //was CV_CHAIN_APPROX_SIMPLE
                RETR_TYPE.CV_RETR_TREE);

            ContourNode tree = new ContourNode(contours);

            Dictionary<Card, Point> cardlocs = new Dictionary<Card, Point>();
            foreach (KeyValuePair<Card, CardContour> pair in GiveCards(tree))
            {
                ContourNode node = pair.Value.Node;
                Card card = pair.Key;

                PointF fcenter = node.Contour.GetMinAreaRect().center;
                Point center = new Point((int)fcenter.X, (int)fcenter.Y);

                cardlocs.Add(card, center);
            }

            #region draw
            #if DEBUG
            TreeViz.VizualizeTree(tree);
            ContourAnalyzer.DrawContours(tree, table);
            //ImageViewer.Show(table);
            #endif
            #endregion

            return cardlocs;
        }

        public static IEnumerable<KeyValuePair<Card, CardContour>> GiveCards(ContourNode tree)
        {
            if (ContourAnalyzer.IsCard(tree))
            {
                CardContour cardcont = new CardContour(tree);
                yield return new KeyValuePair<Card, CardContour>(cardcont.GetCard(), cardcont);
            }
            else
            {
                foreach (ContourNode child in tree.Children)
                {
                    foreach (KeyValuePair<Card, CardContour> pair in GiveCards(child))
                    {
                        yield return pair;
                    }
                }
            }
        }
        #endregion
    }
}
