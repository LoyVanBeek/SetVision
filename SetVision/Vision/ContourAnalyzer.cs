using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SetVision.Gamelogic;
using System.Drawing;
using Emgu.CV.Structure;
using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.CvEnum;

namespace SetVision.Vision
{
    public class ContourAnalyzer : ICardDetector
    {
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
            #region process image
            //Convert the image to grayscale and filter out the noise
            Image<Gray, Byte> gray = table.Convert<Gray, Byte>();

            Gray cannyThreshold = new Gray(180);
            Gray cannyThresholdLinking = new Gray(120);
            Gray circleAccumulatorThreshold = new Gray(120);

            Image<Gray, Byte> cannyEdges = gray.Canny(cannyThreshold, cannyThresholdLinking);
            
            StructuringElementEx el = new StructuringElementEx(3,3, 1,1,CV_ELEMENT_SHAPE.CV_SHAPE_RECT);
            cannyEdges = cannyEdges.MorphologyEx(el, CV_MORPH_OP.CV_MOP_CLOSE, 1);
            #endregion

            Contour<Point> contours = cannyEdges.FindContours(
                CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, //was CV_CHAIN_APPROX_SIMPLE
                RETR_TYPE.CV_RETR_TREE);

            ContourNode tree = new ContourNode(contours);
            LabelTree(tree);

            //StripDoubles(tree);

            ColorizeTree(tree, table);

            Dictionary<Card, Point> cardlocs = new Dictionary<Card, Point>();
            //NOT YET WORKING, DEPENDS ON PREVIOUS CODE
            //foreach (KeyValuePair<Card, ContourNode> pair in GiveCards(tree))
            //{
            //    ContourNode node = pair.Value;
            //    Card card = pair.Key;

            //    PointF fcenter = node.Contour.GetMinAreaRect().center;
            //    Point center = new Point((int)fcenter.X, (int)fcenter.Y);

            //    cardlocs.Add(card, center);
            //}

            #region draw
#if DEBUG
            DrawContours(tree, table);
            TreeViz.VizualizeTree(tree);
            //ImageViewer.Show(table);
#endif
            #endregion

            return cardlocs;
        }

        #region labeling
        private static void LabelTree(ContourNode tree)
        {
            LabelNode(tree);
            foreach (ContourNode child in tree.Children)
            {
                LabelNode(child);
                LabelTree(child); //TODO: should be enabled
            }
        }
        private static void LabelNode(ContourNode node)
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

        #region actual labels
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
                    MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_PLAIN, 1,1);
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
                    canvas.Draw(child.Shape+child.Color.ToString(),
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

        #region color
        private static void ColorizeTree(ContourNode tree, Image<Bgr, Byte> image)
        {
            ColorizeNode(tree, image);
            foreach (ContourNode child in tree.Children)
            {
                ColorizeNode(child, image);
                ColorizeTree(child, image);
            }
        }
        private static void ColorizeNode(ContourNode node, Image<Bgr, Byte> image)
        {
            if (node.Shape != null
                && node.Contour.Area > 100
                && ((node.Shape == Shape.Squiggle)
                    || (node.Shape == Shape.Diamond)
                    || (node.Shape == Shape.Oval)
                    || (node.Shape == Shape.Card)))
            {
                CardColor color = RecognizeColor(image, node);
                node.Color = color;
            }
        }

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
            CardColor color2= CardColor.White;
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

        private static Image<Bgr, Byte> ExtractContourImage(Image<Bgr, Byte> image, Contour<Point> contour, out Image<Gray, Byte> mask)
        {
            mask = image.Convert<Gray, Byte>();
            mask.SetZero();
            //Contour<Point> shifted = ShiftContour(contour, -3,-3);
            mask.Draw(contour, new Gray(255), new Gray(0), 2, -1);

            return image.And(new Bgr(255, 255, 255), mask);
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
            else if (col.Blue > col.Green && col.Red > col.Green)
            {
                //if blue and red are the highest
                return CardColor.Purple;
            }
            else
            {
                return CardColor.Other;
            }
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

        /// <summary>
        /// Removes nodes with the same label and area from the tree, stripping the node out the tree and 
        /// connecting its children to its parent (A-B-C) to (A-C)
        /// </summary>
        /// <param name="tree"></param>
        public static void StripDoubles(ContourNode tree)
        {
            if (tree.Parent != null)
            {
                if ((tree.Parent.Shape == tree.Shape)
                    &&
                    ((tree.Parent.Contour.Area <= (tree.Contour.Area+tree.Contour.Perimeter)) &&
                    (tree.Parent.Contour.Area >= tree.Contour.Area)))
                {
                    //The node called tree is a double:
                    foreach (ContourNode child in tree.Children)
                    {
                        child.Parent = tree.Parent;
                        tree.Parent.Children = tree.Children;
                        tree = null;
                    }
                }
            }
            foreach (ContourNode child in tree.Children)
            {
                StripDoubles(child);
            }
        }

        /* TODO: 
         * If a node has a color and no children, its Solid
         * if it has 1 child with White or Other-color, its Open. 
         * If there is a child with the same color, it Dashed.
         */

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
                    foreach(KeyValuePair<Card, ContourNode> pair in GiveCards(child))
                    {
                        yield return pair;
                    }
                }
            }


            //Stack<ContourNode> stack = new Stack<ContourNode>();
            //stack.Push(tree);

            //ContourNode current = null;
            //while (true)
            //{
            //    while (stack.Count > 0)
            //    {
            //        stack.Push(current);
            //        current = current.Children[0];
            //    }
            //}
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

                Fill fill = DetermineFill(cardNode);

                Shape shape = cardNode.Children[0].Shape;
                
                Card card = new Card(color, shape, fill, count);
                cardNode.Fill = fill;
                return new KeyValuePair<Card, ContourNode>(card, cardNode);
            }
        }

        private static Fill DetermineFill(ContourNode cardNode)
        {
            Fill fill = Fill.Other;
            ContourNode outer = cardNode.Children[0];
            ContourNode inner = outer.Children[0];

            double card2blobColordist = ColorDistance(cardNode.averageBgr, inner.averageBgr);

            //Als distance < 60: dan waarschijnlijk dashed, maar wel in de aangegeven kleur. 
            //Dit moet als eerste gechecked worden
            
            if ((inner.Color == CardColor.Other ||
                    inner.Color == CardColor.Green ||
                    inner.Color == CardColor.Red ||
                    inner.Color == CardColor.Purple)
                &&
                card2blobColordist < 60)
            {
                fill = Fill.Dashed;
            }
            else if (inner.Color == CardColor.Green  || 
                inner.Color == CardColor.Red    || 
                inner.Color == CardColor.Purple)
            {
                fill = Fill.Solid;
            }
            else if (inner.Color == CardColor.White)
            {
                fill = Fill.Open;
            }

            inner.Fill = fill;
            outer.Fill = fill;

            return fill;
        }

        private static double ColorDistance(Bgr a, Bgr b)
        {
            //do an euclidian distance on R, G and B
            double blue = Math.Abs(a.Blue-b.Blue) * Math.Abs(a.Blue-b.Blue);
            double green = Math.Abs(a.Green-b.Green) * Math.Abs(a.Green-b.Green);
            double red = Math.Abs(a.Red-b.Red) * Math.Abs(a.Red-b.Red);
            double result = Math.Sqrt(blue + green + red);
            return result;
        }
    }
}
