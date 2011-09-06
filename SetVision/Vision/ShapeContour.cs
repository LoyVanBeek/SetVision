using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Drawing;
using SetVision.Gamelogic;

namespace SetVision.Vision
{
    public class ShapeContour
    {
        private ContourNode Node;

        private CardContour _card;
        public CardContour Card
        {
            get
            {
                return _card;
            }
        }

        public Shape Shape;

        public ShapeContour(ContourNode node)
        {
            Node = node;
            Shape = GetShape();
            if (Shape == Shape.Other)
            {
                throw new ArgumentException("The contour does not match a shape");
            }
        }
        
        private Fill GetFill()
        {
            return Fill.Other;
        }

        private CardColor GetColor()
        {
            return RecognizeColor();
        }

        private Shape GetShape()
        {
            if (IsCard(this.Node))
            {
                return Shape.Card;
            }
            else if (IsSquiggle(this.Node))
            {
                return Shape.Squiggle;
            }
            else if (IsOval(this.Node))
            {
                return Shape.Oval;
            }
            else if (IsDiamond(this.Node))
            {
                return Shape.Diamond;
            }
            else
            {
                return Shape.Other;
            }
        }

        #region Determine Fill

        #endregion

        #region Color
        private CardColor RecognizeColor()
        {
            /* Set the contour as the ROI of the image
             * Make an empty image on which you fill the inner of the contour. This is the mask. 
             * Now, AND-the mask with the image. 
             * Calculate the average color of the image, for each channel. 
             * 
             * Then, do some thresholding on RGB and/or HSV. The values can be got from paint. 
             * 
             */
            #region extract color
            Image<Bgr, Byte> focusBgr = Node.Image;

            focusBgr = RemoveCardColor(this.Node, focusBgr);

            Bgr avgBgr = new Bgr();
            MCvScalar scr1 = new MCvScalar();
            focusBgr.AvgSdv(out avgBgr, out scr1, Node.AttentionMask);

            Image<Hsv, Byte> focusHsv = focusBgr.Convert<Hsv, Byte>();
            Hsv avgHsv = new Hsv();
            MCvScalar scr2 = new MCvScalar();
            focusHsv.AvgSdv(out avgHsv, out scr2, Node.AttentionMask);
            #endregion

            CardColor color = ClassifyColor(avgBgr, avgHsv);
            ContourNode parent = Node.FindParent(Shape.Card, null, null);
            if (parent != null)
            {
                double colDist = ColorDistance(avgBgr, parent.averageBgr);
            }
            this.Node.averageBgr = avgBgr;
            this.Node.averageHsv = avgHsv;
            #region debug
#if DEBUG
            Image<Bgr, Byte> debug = new Image<Bgr, Byte>(250, 200);
            debug.SetValue(avgBgr);
            string bgrstr =
                    ((int)avgBgr.Red).ToString() + ","
                + ((int)avgBgr.Green).ToString() + ","
                + ((int)avgBgr.Blue).ToString();

            string hsvstr =
                    ((int)avgHsv.Hue).ToString() + ","
                + ((int)avgHsv.Satuation).ToString() + ","
                + ((int)avgHsv.Value).ToString();

            debug = debug.ConcateHorizontal(focusBgr);

            Image<Bgr, Byte> rgbMask = new Image<Bgr, byte>(new Image<Gray, byte>[] { Node.AttentionMask, Node.AttentionMask, Node.AttentionMask });
            //debug = debug.ConcateHorizontal(rgbMask);
            //ImageViewer.Show(debug, "rgb(" + bgrstr + ") hsv(" + hsvstr + ")"+"Classified as "+color.ToString());
#endif
            #endregion

            return color;
        }

        private CardColor ClassifyColor(Bgr avgBgr, Hsv avgHsv)
        {
            if (avgHsv.Satuation < 30)
            {
                return CardColor.White;
            }
            else if (avgHsv.Satuation < 45)
            {
                return CardColor.Other;
            }
            else if (avgBgr.Red > avgBgr.Blue && avgBgr.Red > avgBgr.Green)
            {
                return CardColor.Red;
            }
            else if (avgBgr.Green > avgBgr.Blue && avgBgr.Green > avgBgr.Red)
            {
                return CardColor.Green;
            }
            else if (avgBgr.Green < avgBgr.Blue && avgBgr.Green < avgBgr.Red)
            {
                return CardColor.Purple;
            }
            else
            {
                return CardColor.White;
            }
        }

        private Image<Bgr, Byte> RemoveCardColor(ContourNode node, Image<Bgr, Byte> removeFrom)
        {
            ContourNode parentCard = node.FindParent(Shape.Card, null, null);
            if (parentCard != null)
            {
                //Image<Bgr, Byte> eroded = removeFrom.Erode(1);
                //Image<Bgr, Byte> thresholded = eroded.ThresholdBinary(parentCard.averageBgr, new Bgr(255,255,255));
                //return thresholded;
                Image<Gray, Byte> gray = removeFrom.Convert<Gray, Byte>();
                Image<Bgr, Byte> multiplier = new Image<Bgr, byte>(new Image<Gray, byte>[] { gray, gray, gray });
                Image<Bgr, Byte> mul = removeFrom.Mul(multiplier, 0.015);
                Image<Bgr, Byte> threshed = mul.ThresholdToZeroInv(new Bgr(254, 254, 254));
                Image<Gray, Byte> mask = threshed.Convert<Gray, Byte>();
                Image<Bgr, Byte> result = mul.And(new Bgr(255, 255, 255), mask);
                result = result.Mul(multiplier, 0.005);
                return result;
            }
            else
            {
                return removeFrom;
            }
        }

        private double ColorDistance(Bgr a, Bgr b)
        {
            //do an euclidian distance on R, G and B
            double blue = Math.Abs(a.Blue - b.Blue) * Math.Abs(a.Blue - b.Blue);
            double green = Math.Abs(a.Green - b.Green) * Math.Abs(a.Green - b.Green);
            double red = Math.Abs(a.Red - b.Red) * Math.Abs(a.Red - b.Red);
            double result = Math.Sqrt(blue + green + red);
            return result;
        }
        #endregion

        #region Shape
        private bool IsDiamond(ContourNode node)
        {
            bool inCard = node.Parent != null &&
                   (node.Parent.Shape == Shape.Card ||
                       node.Parent.Shape == Shape.Diamond);
            if (inCard)
            {
                using (MemStorage storage = new MemStorage())
                {
                    Contour<Point> currentContour = node.Contour.ApproxPoly(node.Contour.Perimeter * 0.02, storage);
                    Point[] points = currentContour.ToArray();
                    List<LineSegment2D> contourEdges = new List<LineSegment2D>(PointCollection.PolyLine(points, true));

                    bool sides4 = currentContour.Total == 4;

                    //Image<Bgr, Byte> debug = new Image<Bgr, byte>(700, 700);
                    //debug.DrawPolyline(points, true, new Bgr(0, 255, 0), 1);
                    //ImageViewer.Show(debug, "Sides:" + currentContour.Total.ToString());

                    return sides4;
                }
            }
            else
            {
                return false;
            }
        }

        private bool IsOval(ContourNode node)
        {
            bool inCard = node.Parent != null &&
                   (node.Parent.Shape == Shape.Card ||
                       node.Parent.Shape == Shape.Oval);
            if (inCard)
            {
                #region sharp
                //An oval doesn't have sharp angles
                bool sharp = false;
                bool vertices = false;

                using (MemStorage storage = new MemStorage())
                {
                    Contour<Point> currentContour = node.Contour.ApproxPoly(node.Contour.Perimeter * 0.01, storage);
                    vertices = currentContour.Total > 4;

                    Point[] points = currentContour.ToArray();
                    List<LineSegment2D> edges = new List<LineSegment2D>(PointCollection.PolyLine(points, true));

#if DEBUG
                    #region draw
                    //Image<Bgr, Byte> debug = new Image<Bgr, Byte>(800, 800);
                    //debug.DrawPolyline(points, true, new Bgr(0, 0, 255), 1);
                    #endregion draw
#endif
                    for (int i = 0; i < edges.Count; i++)
                    {
                        double angle = edges[(i + 1) % edges.Count].GetExteriorAngleDegree(edges[i]);
                        if (Math.Abs(angle) > 60)
                        {
                            sharp = true;
                        }

                        //MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_PLAIN, 1, 1);
                        //debug.Draw(((int)angle).ToString(), ref font, edges[i].P2, new Bgr(0, 0, 255));
                    }
                    //ImageViewer.Show(debug, "Sharp:" + sharp.ToString());
                }
                #endregion

                return !sharp && vertices;
            }
            else
            {
                return false;
            }
        }

        private bool IsSquiggle(ContourNode node)
        {
            bool inCard = node.Parent != null &&
                (node.Parent.Shape == Shape.Card ||
                    node.Parent.Shape == Shape.Squiggle);
            bool convex = true;

            if (inCard)
            {
#if DEBUG
                //ShowContour(node.Contour, new Image<Bgr, Byte>(900, 900), "Squiggle");
#endif
                using (MemStorage storage = new MemStorage())
                {
                    Contour<Point> currentContour = node.Contour.ApproxPoly(node.Contour.Perimeter * 0.01, storage);
                    Point[] points = currentContour.ToArray();
                    List<LineSegment2D> edges = new List<LineSegment2D>(PointCollection.PolyLine(points, true));

#if DEBUG
                    #region draw
                    //Image<Bgr, Byte> debug = new Image<Bgr, Byte>(800, 800);
                    //debug.DrawPolyline(points, true, new Bgr(0, 0, 255), 1);
                    //ImageViewer.Show(debug);
                    #endregion draw
#endif
                    //List<double> angles = new List<double>(edges.Count);
                    for (int i = 0; i < edges.Count; i++)
                    {
                        double angle = edges[(i + 1) % edges.Count].GetExteriorAngleDegree(edges[i]);
                        //angles.Add(angle);
                        if (angle < 0)
                        {
                            convex = false;
                            break;
                        }

                        //MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_PLAIN, 1, 1);
                        //debug.Draw(((int)angle).ToString(), ref font, edges[i].P2 ,new Bgr(0, 0, 255));
                    }
                    //angles.Sort();
                    //if (angles[0] < 45)
                    //{
                    //    convex = false;
                    //}
                    //ImageViewer.Show(debug);
                }
            }
            return !convex && inCard;
        }

        private bool IsCard(ContourNode node)
        {
            bool area = node.Contour.Area > 5000;
            MCvBox2D bounds = node.Contour.GetMinAreaRect();
            float ratio =
                ((bounds.size.Width / bounds.size.Height)
                +
                (bounds.size.Height / bounds.size.Width));
            //ratio is supposed to be 2.16667

            bool ratioOK = (ratio > 2.0 && ratio < 2.3);
            //#if DEBUG
            //if (area)
            //{
            //    ShowContour(node.Contour, new Image<Bgr, Byte>(900, 900), "CARD");
            //}
            //else
            //{
            //    ShowContour(node.Contour, new Image<Bgr, Byte>(900, 900), "");
            //}
            //#endif
            return ratioOK && area;

        }
        #endregion Shape
    }
}
