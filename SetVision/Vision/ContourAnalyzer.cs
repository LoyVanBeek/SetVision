using System;
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using SetVision.Gamelogic;
using SetVision.Learning;

namespace SetVision.Vision
{
    public class ContourAnalyzer : ICardDetector
    {
        static BgrHsvClassifier classifier;

        static BgrClassifier bgrClassifier;
        static HsvClassifier hsvClassifier;

        /// <summary>
        /// LocateCards works by analyzing the contours in the image. 
        /// For instance, the Diamond in Set is a polygon with exactly 4 vertices. 
        /// The Oval has no such features yet. 
        /// The Squiggle is not convex, but concave and has edges in a 'right bend', instead of only 'left bends'
        /// 
        /// All these shapes are inside the contour of a (white) card, which is a rounded square. 
        /// Cards may also be the (only) exterior boundaries
        /// </summary>
        /// <param name="table">An image displaying the table with the Set cards</param>
        /// <returns>A dict locating which cards are present where in the image</returns>
        public Dictionary<Card, Point> LocateCards(Image<Bgr, Byte> table)
        {
            classifier = new BgrHsvClassifier();
            classifier.Train();

            bgrClassifier = new BgrClassifier();
            hsvClassifier = new HsvClassifier();

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

            AssignShapes(tree);
            AssignImages(tree, table, true);

            //var debug1 = table.Clone();
            //DrawContours(tree, debug1);
            //ImageViewer.Show(debug1);

            FilterTree(tree);

            //var debug2 = table.Clone();
            //DrawContours(tree, debug2);
            //ImageViewer.Show(debug2);

            AssignColors(tree, table);
            //TreeViz.VizualizeTree(tree);
            AssignFills(tree);

            Dictionary<Card, Point> cardlocs = new Dictionary<Card, Point>();
            foreach (KeyValuePair<Card, ContourNode> pair in GiveCards(tree))
            {
                ContourNode node = pair.Value;
                Card card = pair.Key;

                PointF fcenter = node.Contour.GetMinAreaRect().center;
                Point center = new Point((int)fcenter.X, (int)fcenter.Y);

                cardlocs.Add(card, center);
            }

            #region draw and debug
#if DEBUG
            //DrawContours(tree, table);
            //TreeViz.VizualizeTree(tree);
            //ImageViewer.Show(table);
#endif
            #endregion

            return cardlocs;
        }

        #region shaping
        private static void AssignShapes(ContourNode tree)
        {
            AssignShape(tree);
            foreach (ContourNode child in tree.Children)
            {
                AssignShape(child);
                AssignShapes(child); //TODO: should be enabled
            }
        }
        private static void AssignShape(ContourNode node)
        {
            if (IsCard(node))
            {
                node.Shape = Shape.Card;
            }
            else if (IsSquiggle(node))
            {
                node.Shape = Shape.Squiggle;
            }
            else if (IsOval(node))
            {
                node.Shape = Shape.Oval;
            }
            else if (IsDiamond(node))
            {
                node.Shape = Shape.Diamond;
            }
            else
            {
                node.Shape = Shape.Other;
            }
        }
        #endregion

        #region actual shapes
        public static bool IsDiamond(ContourNode node)
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

        public static bool IsOval(ContourNode node)
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

        public static bool IsSquiggle(ContourNode node)
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

        public static bool IsCard(ContourNode node)
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
        #endregion

        #region drawing
        public static void DrawContours(ContourNode node, Image<Bgr, Byte> canvas, System.Drawing.Color color)
        {
            Bgr _color = new Bgr(System.Drawing.Color.Red);

            foreach (ContourNode child in node.Children)
            {
                canvas.DrawPolyline(child.Contour.ToArray(), true, _color, 1);

                if (node.Shape != null)
                {
                    MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_PLAIN, 1, 1);
                    canvas.Draw(child.Shape + child.Color.ToString(),
                        ref font,
                        child.Contour[0],
                        new Bgr(System.Drawing.Color.Red)
                        );
                }

                DrawContours(child, canvas, color);
            }
        }

        public static void DrawContours(ContourNode node, Image<Bgr, Byte> canvas)
        {
            #region color
            Random rnd = new Random(node.Contour.Ptr.ToInt32());
            Bgr _color = new Bgr(rnd.Next(255), rnd.Next(255), rnd.Next(255));
            #endregion

            foreach (ContourNode child in node.Children)
            {
                canvas.DrawPolyline(child.Contour.ToArray(), true, _color, 1);

                if (node.Shape != null)
                {
                    MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_PLAIN, 1, 1);
                    canvas.Draw(child.Shape + child.Color.ToString(),
                        ref font,
                        child.Contour[0],
                        new Bgr(System.Drawing.Color.Red)
                        );
                }

                DrawContours(child, canvas);
            }
        }

        public static void ShowContour(Contour<Point> contour, Image<Bgr, Byte> original_image, string message)
        {
            Image<Bgr, Byte> canvas = original_image.Clone();
            canvas.SetZero();

            canvas.DrawPolyline(contour.ToArray(), true, new Bgr(255, 255, 255), 1);
            ImageViewer.Show(canvas, message);
        }
        #endregion

        #region images
        private static void AssignImages(ContourNode tree, Image<Bgr, Byte> image, bool setROI)
        {
            AssignImage(tree, image, setROI);
            foreach (ContourNode child in tree.Children)
            {
                AssignImage(child, image, setROI);
                AssignImages(child, image, setROI);
            }
        }

        private static void AssignImage(ContourNode node, Image<Bgr, Byte> image, bool setROI)
        {
            node.Image = ExtractContourImage(image, node.Contour, out node.AttentionMask);
            if (setROI)
            {
                node.Image.ROI = node.Contour.BoundingRectangle;
                node.AttentionMask.ROI = node.Contour.BoundingRectangle;
            }
        }

        private static Image<Bgr, Byte> ExtractContourImage(Image<Bgr, Byte> image, Contour<Point> contour, out Image<Gray, Byte> mask)
        {
            mask = image.Convert<Gray, Byte>();
            mask.SetZero();
            //Contour<Point> shifted = ShiftContour(contour, -3,-3);
            mask.Draw(contour, new Gray(255), new Gray(0), 2, -1);

            return image.And(new Bgr(255, 255, 255), mask);
        }
        #endregion

        #region color
        private static void AssignColors(ContourNode tree, Image<Bgr, Byte> image)
        {
            AssignColor(tree, image);
            foreach (ContourNode child in tree.Children)
            {
                AssignColor(child, image);
                AssignColors(child, image);
            }
        }
        private static void AssignColor(ContourNode node, Image<Bgr, Byte> image)
        {
            if (node.Contour.Area > 100
                && ((node.Shape == Shape.Squiggle)
                    || (node.Shape == Shape.Diamond)
                    || (node.Shape == Shape.Oval)))
            {
                //CardColor color = RecognizeColor(image, node);
                //CardColor color = RecognizeColor(node);
                CardColor color = RecognizeColor2(node);
                node.Color = color;

                AssignAverageColors(node);
            }
            else if (node.Shape == Shape.Card)
            {
                AssignAverageColors(node);
            }

        }

        private static void AssignAverageColors(ContourNode node)
        {
            //Then get the average color of the card (should be white or gray)
            Bgr avgBgr = new Bgr();
            MCvScalar scr1 = new MCvScalar();
            node.Image.AvgSdv(out avgBgr, out scr1, node.AttentionMask);

            node.averageBgr = avgBgr;

            Hsv avgHsv = new Hsv();
            MCvScalar scr2 = new MCvScalar();
            node.Image.Convert<Hsv, Byte>().AvgSdv(out avgHsv, out scr2, node.AttentionMask);

            node.averageBgr = avgBgr;
            node.averageHsv = avgHsv;
        }

        #region old method
        private static CardColor RecognizeColor(Image<Bgr, Byte> _image, ContourNode node)
        {
            /* Set the contour as the ROI of the image
             * Make an empty image on which you fill the inner of the contour. This is the mask. 
             * Now, AND-the mask with the image. 
             * Calculate the average color of the image, for each channel. 
             * 
             * Then, do some thresholding on RGB and/or HSV. The values can be got from paint. 
             * 
             */
            Contour<Point> contour = node.Contour;

            #region extract color
            Image<Gray, Byte> mask;
            Image<Bgr, Byte> focusBgr = ExtractContourImage(_image, contour, out mask);

            Image<Bgr, Byte> colfinder = focusBgr; // RemoveCardColor(node, focusBgr);

            Bgr avgBgr = new Bgr();
            MCvScalar scr1 = new MCvScalar();
            colfinder.AvgSdv(out avgBgr, out scr1, mask);

            Image<Hsv, Byte> focusHsv = colfinder.Convert<Hsv, Byte>();
            Hsv avgHsv = new Hsv();
            MCvScalar scr2 = new MCvScalar();
            focusHsv.AvgSdv(out avgHsv, out scr2, mask);
            #endregion

            CardColor color = ClassifyColor(avgBgr, avgHsv);
            CardColor color2 = CardColor.White;
            if (!isGray(avgBgr, 20))
            {
                color2 = ClassifyBgr(avgBgr);
            }

            ContourNode parent = node.FindParent(Shape.Card, null, null);
            double colDist = double.MaxValue;
            if (parent != null)
            {
                colDist = ColorDistance(avgBgr, parent.averageBgr);
            }
            node.averageBgr = avgBgr;
            node.averageHsv = avgHsv;
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

            Image<Bgr, Byte> rgbMask = new Image<Bgr, byte>(new Image<Gray, byte>[] { mask, mask, mask });
            //debug = debug.ConcateHorizontal(rgbMask);
            //ImageViewer.Show(debug, "rgb(" + bgrstr + ") hsv(" + hsvstr + ")"+"Classified as "+color.ToString());
#endif
            #endregion

            focusBgr.ROI = contour.GetMinAreaRect().MinAreaRect();
            node.Image = focusBgr;

            return color;
        }

        private static CardColor ClassifyColor(Bgr avgBgr, Hsv avgHsv)
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

        private static void ShiftContour(ref Contour<Point> orig, int x, int y)
        {
            //for (int i = 0; i <= orig.Count<Point>()-1; i++)
            //{
            //    Point p = orig[i];
            //    p.X += x;
            //    p.Y += y;
            //    orig[i] = p;
            //}

            //using (MemStorage storage = new MemStorage())
            //{
            //    Contour<Point> result = new Contour<Point>(storage);
            //    foreach (Point p in orig)
            //    {
            //        result.Push(new Point(p.X + x, p.Y + y));
            //    }

            //    result.HNext = orig.HNext;
            //    result.HPrev = orig.HPrev;
            //    result.VNext = orig.VNext;
            //    result.VPrev = orig.VPrev;

            //    return result;
            //}

            //using (MemStorage storage = new MemStorage())
            //{
            //    Contour<Point> result = new Contour<Point>(storage);
            //    result = orig;
            //    //result.HNext = orig.HNext;
            //    Point p = result.PopFront();
            //    while(p != null)
            //    {
            //        result.Push(new Point(p.X + x, p.Y + y));
            //        p = result.Pop();
            //    }
            //    return result;
            //}
        }

        private static Image<Bgr, Byte> RemoveCardColor(ContourNode node, Image<Bgr, Byte> removeFrom)
        {
            ContourNode parentCard = node.FindParent(Shape.Card, null, null);
            if (parentCard != null)
            {
                //Image<Bgr, Byte> eroded = removeFrom.Erode(1);
                //Image<Bgr, Byte> thresholded = eroded.ThresholdBinary(parentCard.averageBgr, new Bgr(255,255,255));
                //return thresholded;
                Image<Gray, Byte> gray = removeFrom.Convert<Gray, Byte>();

                Image<Bgr, Byte> subbed = removeFrom.AbsDiff(parentCard.averageBgr);
                Image<Gray, Byte> gray2 = subbed.Convert<Gray, Byte>();

                double[] mins, maxs;
                Point[] minlocs, maxlocs;
                gray2.MinMax(out mins, out maxs, out minlocs, out maxlocs);
                Point maxloc = maxlocs[0];
                Bgr colorAtMaxloc = removeFrom[maxloc];
                Image<Bgr, Byte> maxcolor = removeFrom.Clone();
                maxcolor.SetValue(colorAtMaxloc);

                Image<Bgr, Byte> multiplier = new Image<Bgr, byte>(new Image<Gray, byte>[] { gray, gray, gray });
                Image<Bgr, Byte> mul = removeFrom.Mul(multiplier, 0.015);
                Image<Bgr, Byte> threshed = mul.ThresholdToZeroInv(new Bgr(254, 254, 254));
                Image<Gray, Byte> mask = threshed.Convert<Gray, Byte>();
                Image<Bgr, Byte> result = mul.And(new Bgr(255, 255, 255), mask);

                multiplier._Erode(1);
                result = result.Mul(multiplier, 0.005);

                double[] mins2, maxs2;
                Point[] minlocs2, maxlocs2;
                result.MinMax(out mins2, out maxs2, out minlocs2, out maxlocs2);
                double max2 = maxs2[0];
                double scale = 255 / max2;
                Image<Bgr, Byte> maxxed = result.Mul(scale);
                maxxed._Dilate(1);
                return maxxed;

                //return maxcolor;
            }
            else
            {
                return removeFrom;
            }
        }

        private static CardColor ClassifyBgr(Bgr col)
        {
            if (col.Red > col.Blue && col.Red > col.Green)
            //if red has the highest value
            {
                return CardColor.Red;
            }
            else if (col.Green > col.Blue && col.Green > col.Red)
            {
                //if green has the highest value
                return CardColor.Green;
            }
            else if (col.Blue > col.Green && col.Red > col.Green
                && Math.Abs(col.Blue - col.Red) < 30) //There must be about the same amount of R and B
            {
                //if blue and red are the highest
                return CardColor.Purple;
            }
            else
            {
                return CardColor.Other;
            }
        }
        #endregion

        #region new method
        private static CardColor RecognizeColor(ContourNode node)
        {
            //First, flatten the images of all children to 1 image which we can analyze
            Image<Bgr, Byte> flat = Flatten(node);
            flat.ROI = node.Contour.BoundingRectangle;

            Bgr bgr; MCvScalar scalar;
            flat.AvgSdv(out bgr, out scalar);

            Image<Hsv, Byte> hsvFlat = flat.Convert<Hsv, Byte>();
            Hsv hsv; MCvScalar scalar2;
            hsvFlat.AvgSdv(out hsv, out scalar2);

            CardColor colorHsv = ClassifyHsv(hsv);
            CardColor colorBgr = ClassifyBgr(bgr);
            CardColor verdict = colorHsv;
            if (verdict == CardColor.Other)
            {
                if (!isGray(bgr, 10))
                {
                    verdict = colorBgr;
                }
                else
                {
                    verdict = CardColor.White;
                }
            }
            if (true)
            {
                Image<Bgr, Byte> debug = new Image<Bgr, byte>(500, 200);
                debug.SetValue(bgr);
                string BgrStr = String.Format("B{0}, G{1}, R{2}={3}", (int)bgr.Blue, (int)bgr.Green, (int)bgr.Red, colorBgr.ToString());
                string HsvStr = String.Format("H{0}, S{1}, V{2}={3}", (int)hsv.Hue, (int)hsv.Satuation, (int)hsv.Value, colorHsv.ToString());
                string total = BgrStr + " - " + HsvStr + " VERDICT=" + verdict.ToString();

                MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_PLAIN, 1, 1);
                debug.Draw(total, ref font, new Point(20, 20), new Bgr(0, 0, 0));
                //ImageViewer.Show(debug, total);

                debug.Save("colordebug/" + total + ".png");
            }
            return verdict;
        }

        private static CardColor ClassifyHsv(Hsv hsv)
        {
            //See https://secure.wikimedia.org/wikipedia/en/wiki/HSL_and_HSV
            /*
             * Green: 60-180 degrees
             * Red: 330-60 degrees
             * Purple: +/- 300 degrees, e.g. 270-330 
             * 
             * But emgu.cv.structure.hsv does not work in degrees, but in bytes, so maxval = 255.
             * 
             * So H-values for each color:
             * Green:   83, 71, 80, 80, 80      (65 - 100)
             * Card:    105, 103                (100 - 115)
             * Purple:  119, 126, 131, 135, 124 (115 - 145)
             * Red:     152, 172, 169, 170, 153 (145 - 180)
             */

            bool vague = hsv.Satuation < 90;

            bool maybeGreen = hsv.Hue > 60 && hsv.Hue <= 70;
            bool maybeRed = hsv.Hue > 125 && hsv.Hue <= 155;
            bool maybePurple = hsv.Hue > 75 && hsv.Hue <= 100;
            bool maybeWhite = hsv.Hue > 100 && hsv.Hue <= 110;

            bool vagueRed = hsv.Hue > 155 && hsv.Hue <= 171;
            bool vaguePurple = hsv.Hue > 115 && hsv.Hue <= 157;
            bool vagueGreen = hsv.Hue > 120 && hsv.Hue <= 130;

            if (!vague)
            {
                #region colors are clear
                if (maybeGreen)
                {
                    return CardColor.Green;
                }
                else if (maybeRed || vagueRed)
                {
                    return CardColor.Red;
                }
                else if (maybePurple)
                {
                    return CardColor.Purple;
                }
                else if (maybeWhite)
                {
                    return CardColor.White;
                }
                else
                {
                    return CardColor.Other;
                }
                #endregion
            }
            else
            {
                if (vaguePurple)
                {
                    return CardColor.Purple;
                }
                else
                {
                    return CardColor.Other;
                }
            }
        }

        private static Image<Bgr, Byte> Flatten(ContourNode tree)
        {
            Image<Bgr, Byte> flat = new Image<Bgr, byte>(tree.Image.Size);

            flat = flat.Add(tree.Image);
            foreach (ContourNode child in tree.Children)
            {
                flat = flat.Add(Flatten(child));
            }
            return flat;
        }
        #endregion

        #region decide on best colored pixel
        private static CardColor RecognizeColor2(ContourNode node)
        {
            //First, flatten the images of all children to 1 image which we can analyze
            Image<Bgr, Byte> image = node.Image;//Flatten(node);
            image.ROI = node.Contour.BoundingRectangle;

            #region get average
            Bgr bgr; MCvScalar scalar;
            image.AvgSdv(out bgr, out scalar);

            Image<Hsv, Byte> hsvFlat = image.Convert<Hsv, Byte>();
            Hsv hsv; MCvScalar scalar2;
            hsvFlat.AvgSdv(out hsv, out scalar2);
            #endregion

            Point bestpos = FindBestColoredPixel(image);
            bgr = image[bestpos];
            hsv = hsvFlat[bestpos];

            //KINDA WORKS, when used with the Pass 4 training folder, makes some mistakes. 
            //Makes no yet detected mistakes with (more extensive) Pass 9 folder
            //CardColor colorHsv = classifier.Classify(hsv); 
            CardColor colorBgr = classifier.Classify(bgr); 
            //CardColor test = classifier.Classify(bgr); 

            //DOES NOT WORK for some reason
            //CardColor colorHsv = hsvClassifier.Classify(hsv);
            //CardColor colorBgr = bgrClassifier.Classify(bgr);
            //bgrClassifier.Classify(bgr);

            //Trying something out
            //CardColor colorBgr = ClassifyBgr(bgr);
            CardColor colorHsv = colorBgr; //part of tryout
            
            CardColor verdict = colorHsv;
            if (verdict == CardColor.Other)
            {
                if (!isGray(bgr, 10))
                {
                    verdict = colorBgr;
                }
                else
                {
                    verdict = CardColor.White;
                }
            }
            #region save for training
            if (true)
            {
                Image<Bgr, Byte> debug = new Image<Bgr, byte>(500, 200);
                debug.SetValue(bgr);
                string BgrStr = String.Format("B{0}, G{1}, R{2}={3}", (int)bgr.Blue, (int)bgr.Green, (int)bgr.Red, colorBgr.ToString());
                string HsvStr = String.Format("H{0}, S{1}, V{2}={3}", (int)hsv.Hue, (int)hsv.Satuation, (int)hsv.Value, colorHsv.ToString());
                string total = BgrStr + " - " + HsvStr + " VERDICT=" + verdict.ToString();

                MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_PLAIN, 1, 1);
                debug.Draw(total, ref font, new Point(20, 20), new Bgr(0, 0, 0));
                //ImageViewer.Show(debug, total);

                //debug.Save("colordebug/" + total + ".png");
            }
            #endregion
            return verdict;
        }
        #endregion

        public static Point FindBestColoredPixel(Image<Bgr, Byte> image)
        {
            Image<Hsv, Byte> hsvImage = image.Convert<Hsv, Byte>();

            double[] mins, maxs;
            Point[] minlocs, maxlocs;
            Image<Gray, Byte>[] channels = hsvImage.Split();
            Image<Gray, Byte> hChannel = channels[0];
            Image<Gray, Byte> sChannel = channels[1];
            Image<Gray, Byte> vChannel = channels[2];

            vChannel._Not();
            Image<Gray, Byte> use = vChannel.Mul(sChannel, 0.01);

            use.MinMax(out mins, out maxs, out minlocs, out maxlocs);

            Point max = maxlocs[0];

            #region debug
            Bgr col = image[max];
            Image<Bgr, Byte> debug = image.Clone();
            debug.Draw(new CircleF(new PointF(max.X, max.Y), 5), new Bgr(255, 255, 255), 1);
            debug.Draw(new CircleF(new PointF(max.X, max.Y), 6), col, 2);
            debug.Draw(new CircleF(new PointF(max.X, max.Y), 8), new Bgr(255, 255, 255), 1);
            //ImageViewer.Show(debug);
            #endregion

            return max;
        }

        /// <summary>
        /// Check if R, G and B are closer than range together
        /// </summary>
        /// <param name="col"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        private static bool isGray(Bgr col, double range)
        {
            double bg = Math.Abs(col.Blue - col.Green);
            double gr = Math.Abs(col.Green - col.Red);
            double rb = Math.Abs(col.Red - col.Blue);

            bool bgOK = (bg <= range);
            bool grOK = (gr <= range);
            bool rbOK = (rb <= range);

            return bgOK && grOK && rbOK;
        }

        #endregion

        #region fill
        private static void AssignFills(ContourNode tree)
        {
            AssignFill(tree);
            foreach (ContourNode child in tree.Children)
            {
                AssignFill(child);
                AssignFills(child);
            }
        }

        private static void AssignFill(ContourNode node)
        {
            if (node.Shape == Shape.Card)
            {
                node.Fill = DetermineFill2(node);
            }
        }

        private static Fill DetermineFill(ContourNode cardNode)
        {
            Fill fill = Fill.Other;
            ContourNode outer = cardNode.Children[0];
            ContourNode inner = outer.Children[0];

            double card2blobBGRdist = ColorDistance(cardNode.averageBgr, inner.averageBgr);
            double card2blobHSVdist = ColorDistance(cardNode.averageHsv, inner.averageHsv);

            //Als card2blobBGRdist < 60: dan waarschijnlijk dashed, maar wel in de aangegeven kleur. 
            //Dit moet als eerste gechecked worden

            if ((inner.Color == CardColor.Other ||
                    inner.Color == CardColor.Green ||
                    inner.Color == CardColor.Red ||
                    inner.Color == CardColor.Purple)
                &&
                card2blobBGRdist < 60)
            {
                fill = Fill.Dashed;
            }
            else if (inner.Color == CardColor.Green ||
                inner.Color == CardColor.Red ||
                inner.Color == CardColor.Purple)
            {
                fill = Fill.Solid;
            }
            else if (inner.Color == CardColor.White)
            {
                fill = Fill.Open;
            }
            else
            {
                fill = Fill.Other;
            }

            inner.Fill = fill;
            outer.Fill = fill;

            return fill;
        }

        private static Fill DetermineFill2(ContourNode cardNode)
        {
            Fill fill = Fill.Other;
            ContourNode outer = cardNode.Children[0];
            try
            {
                ContourNode inner = outer.Children[0];

                double bgrDist = ColorDistance(cardNode.averageBgr, inner.averageBgr);
                double hsvDist = ColorDistance(cardNode.averageHsv, inner.averageHsv);


                if (hsvDist < 20)
                {
                    fill = Fill.Open;
                }
                else if (hsvDist > 100)
                {
                    fill = Fill.Solid;
                }
                else if (isDashed(inner))
                {
                    fill = Fill.Dashed;
                }

                outer.Fill = fill;
                inner.Fill = fill;
            }
            catch (ArgumentOutOfRangeException)
            {
                //The inner-node has nu children, which indicates a solid node, as being solid doesnt make edges
                fill = Fill.Solid;
            }
            return fill;
        }

        private static bool isDashed(ContourNode inner)
        {
            Image<Bgr, Byte> im = inner.Image.Clone();
            Rectangle oldroi = inner.Image.ROI;
            //TODO: make this percentage-wise
            #region old
            //Rectangle newroi = new Rectangle(oldroi.X + 20, oldroi.Y + 10, oldroi.Width - 40, oldroi.Height - 20); 
            #endregion

            #region new
            double scale = 0.33;
            Point center = new Point(oldroi.X + oldroi.Width / 2, oldroi.Y + oldroi.Height / 2);
            Size size = new Size((int)(oldroi.Size.Width * scale), (int)(oldroi.Size.Height * scale));
            MCvBox2D box = new MCvBox2D(center, size, 0);
            Rectangle newroi = box.MinAreaRect();
            #endregion
            im.ROI = newroi;

            Image<Bgr, float> laplace = im.Laplace(1);

            double[] mins, maxs;
            Point[] minlocs, maxlocs;
            laplace.MinMax(out mins, out maxs, out minlocs, out maxlocs);

            return (maxs[0] > 15);
        }

        private static double ColorDistance(Bgr a, Bgr b)
        {
            //do an euclidian distance on R, G and B
            double blue = Math.Abs(a.Blue - b.Blue) * Math.Abs(a.Blue - b.Blue);
            double green = Math.Abs(a.Green - b.Green) * Math.Abs(a.Green - b.Green);
            double red = Math.Abs(a.Red - b.Red) * Math.Abs(a.Red - b.Red);
            double result = Math.Sqrt(blue + green + red);
            return result;
        }
        private static double ColorDistance(Hsv a, Hsv b)
        {
            //do an euclidian distance on R, G and B
            double blue = Math.Abs(a.Hue - b.Hue) * Math.Abs(a.Hue - b.Hue);
            double green = Math.Abs(a.Satuation - b.Satuation) * Math.Abs(a.Satuation - b.Satuation);
            double red = Math.Abs(a.Value - b.Value) * Math.Abs(a.Value - b.Value);
            double result = Math.Sqrt(blue + green + red);
            return result;
        }
        #endregion

        public static IEnumerable<KeyValuePair<Card, ContourNode>> GiveCards(ContourNode tree)
        {
            if (tree.Shape == Shape.Card)
            {
                yield return AnalyzeNode(tree);
            }
            else
            {
                foreach (ContourNode child in tree.Children)
                {
                    foreach (KeyValuePair<Card, ContourNode> pair in GiveCards(child))
                    {
                        yield return pair;
                    }
                }
            }
        }
        public static KeyValuePair<Card, ContourNode> AnalyzeNode(ContourNode cardNode)
        {
            if (cardNode.Shape != Shape.Card)
            {
                throw new ArgumentException("The node is not labeled as a Card", "node.Shape");
            }
            else
            {
                int count = cardNode.Children.Count;
                CardColor color = cardNode.Children[0].Color;
                Shape shape = cardNode.Children[0].Shape;
                Card card = new Card(color, shape, cardNode.Fill, count);

                cardNode.Color = color; //Set this, the card's background is actually white, but this color matters for the game and in debugging

                return new KeyValuePair<Card, ContourNode>(card, cardNode);
            }
        }

        private static void FilterTree(ContourNode tree)
        {
            FilterNode(tree);
            foreach (ContourNode child in tree.Children)
            {
                FilterNode(child);
                FilterTree(child);
            }
        }

        private static void FilterNode(ContourNode node)
        {
            ContourNode card = node.FindParent(Shape.Card, null, null);

            int minArea = 1000;
            if (card != null)
            {
                minArea = (int)card.Contour.Area / 20;
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                ContourNode child = node.Children[i];
                if (child.Contour.Area < minArea)
                {
                    node.Children.Remove(child);
                }
            }
        }
    }
}
