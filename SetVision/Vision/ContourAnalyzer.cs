using System;
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using SetVision.Gamelogic;
using SetVision.Learning;
using SetVision.Exceptions;
using System.Linq;

namespace SetVision.Vision
{
    public class ContourAnalyzer : ICardDetector
    {
        static BgrHsvClassifier classifier;

        static CsvWriter writer = new CsvWriter(@"D:\Development\OpenCV\SetVision\SetVision\bin\Debug\colordebug\record.csv");

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
        public Dictionary<Card, Point> LocateCards(Image<Bgr, Byte> table, Settings settings)
        {
            classifier = new BgrHsvClassifier();
            classifier.Train();

            #region process image
            //Convert the image to grayscale and filter out the noise
            Image<Gray, Byte> gray = table.Convert<Gray, Byte>();

            
            Gray cannyThreshold = new Gray(50); //180
            Gray cannyThresholdLinking = new Gray(30); //120
            Gray circleAccumulatorThreshold = new Gray(100); //120
            #region old
		            Image<Gray, Byte> cannyEdges = gray.Canny(cannyThreshold, cannyThresholdLinking);
            if (settings.debuglevel >= 3)
            {
                ImageViewer.Show(cannyEdges, "cannyEdges before Closing");
            } 
	        #endregion
            //#region new
            //Image<Gray, Byte> thresholded = new Image<Gray, byte>(gray.Size);
            //CvInvoke.cvAdaptiveThreshold(gray.Ptr, thresholded.Ptr,
            //    255,
            //    ADAPTIVE_THRESHOLD_TYPE.CV_ADAPTIVE_THRESH_GAUSSIAN_C,
            //    THRESH.CV_THRESH_BINARY_INV, 9, 5);
            //StructuringElementEx el1 = new StructuringElementEx(3, 3, 1, 1, CV_ELEMENT_SHAPE.CV_SHAPE_RECT);
            //thresholded = thresholded.Erode(1);//thresholded.MorphologyEx(el1, CV_MORPH_OP.CV_MOP_CLOSE, 1);//
            //Image<Gray, Byte> cannyEdges = thresholded;//.Canny(new Gray(1), new Gray(10)); 
            //#endregion

            //StructuringElementEx el = new StructuringElementEx(5, 5, 2, 2, CV_ELEMENT_SHAPE.CV_SHAPE_RECT);
            StructuringElementEx el = new StructuringElementEx(3, 3, 1, 1, CV_ELEMENT_SHAPE.CV_SHAPE_RECT);
            cannyEdges = cannyEdges.MorphologyEx(el, CV_MORPH_OP.CV_MOP_CLOSE, 1);
            if (settings.debuglevel >= 3)
            {
                ImageViewer.Show(cannyEdges, "cannyEdges after Closing");
            }
            #endregion

            Contour<Point> contours = cannyEdges.FindContours(
                CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, //was CV_CHAIN_APPROX_SIMPLE
                RETR_TYPE.CV_RETR_TREE);

            ContourNode tree = new ContourNode(contours);

            FilterTree(tree);
            #region debug
            if (settings.debuglevel >= 3)
            {
                var debug = table.Clone();
                DrawContours(tree, debug);
                ImageViewer.Show(debug, "Contours after filtering");
            } 
            #endregion

            AssignShapes(tree);
            AssignImages(tree, table, true);
            #region debug
            if (settings.debuglevel >= 3)
            {
                var debug1 = table.Clone();
                DrawContours(tree, debug1);
                ImageViewer.Show(debug1);
            } 
            #endregion

            FilterTree(tree);

            #region debug
            if (settings.debuglevel >= 3)
            {
                var debug2 = table.Clone();
                DrawContours(tree, debug2);
                ImageViewer.Show(debug2);
            } 
            #endregion

            AssignColors(tree, table, settings);

            #region debug
            if (settings.debuglevel >= 3)
            {
                TreeViz.VizualizeTree(tree);
            } 
            #endregion
            
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

            #region debug
            if (settings.debuglevel >= 1)
            {
                TreeViz.VizualizeTree(tree);
            }
            if (settings.debuglevel >= 2)
            {
                DrawContours(tree, table);
                ImageViewer.Show(table);
            }
            #endregion
            return cardlocs;
        }

        #region shaping
        private static void AssignShapes(ContourNode tree)
        {
            AssignShape(tree);
            foreach (ContourNode child in tree.Children)
            {
                //AssignShape(child);
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
        #endregion

        #region images
        private static void AssignImages(ContourNode tree, Image<Bgr, Byte> image, bool setROI)
        {
            AssignImage(tree, image, setROI);
            foreach (ContourNode child in tree.Children)
            {
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
            mask.Draw(contour, new Gray(255), new Gray(0), 2, -1);

            return image.And(new Bgr(255, 255, 255), mask);
        }
        #endregion

        #region color
        private static void AssignColors(ContourNode tree, Image<Bgr, Byte> image, Settings settings)
        {
            AssignColor(tree, image, settings);
            foreach (ContourNode child in tree.Children)
            {
                //AssignColor(child, image, settings);
                AssignColors(child, image, settings);
            }
        }
        
        private static void AssignColor(ContourNode node, Image<Bgr, Byte> image, Settings settings)
        {
            if (node.Contour.Area > 100
                && ((node.Shape == Shape.Squiggle)
                    || (node.Shape == Shape.Diamond)
                    || (node.Shape == Shape.Oval)))
            {
                //CardColor color = RecognizeColor(image, node);
                //CardColor color = RecognizeColor(node);
                CardColor color = RecognizeColor(node, settings);
                node.Color = color;

                AssignAverageColors(node, settings);
            }
            else if (node.Shape == Shape.Card)
            {
                AssignAverageColors(node, settings);
            }

        }

        private static void AssignAverageColors(ContourNode node, Settings settings)
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

        #region classification
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

        private static CardColor ClassifyBgr2(Bgr col)
        {
            bool red1 = col.Red > 130;
            bool green1 = col.Red < 130;
            bool purple1 = col.Red < 130;

            bool red2 = col.Red > col.Blue && col.Red > col.Green;
            bool green2 = (col.Green > col.Blue && col.Green > col.Red);
            bool purple2 = (col.Blue > col.Green && col.Red > col.Green);

            //bool red3 = col.Red > col.Blue && col.Red > col.Green;
            //bool green3 = (col.Green > col.Blue && col.Green > col.Red);
            //bool purple3 = (col.Blue > col.Green && col.Red > col.Green);

            if (red1 && red2)
            {
                return CardColor.Red;
            }
            else if (purple1 && purple2)
            {
                return CardColor.Purple;
            }
            else if (green1 && green2)
            {
                return CardColor.Green;
            }
            else
            {
                return CardColor.Other;
            }
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
        
        /// <summary>
        /// Check if R, G and B are closer than range together
        /// </summary>
        /// <param name="col">the color to classify as gray on not</param>
        /// <param name="range">The max difference between channels to still classifiy as gray</param>
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

        #region decide on best colored pixel
        private static CardColor RecognizeColor(ContourNode node, Settings settings)
        {
            Image<Bgr, Byte> image = node.Image;
            image.ROI = node.Contour.BoundingRectangle;

            Image<Bgr, Byte> debugBest = null;
            Point bestpos = FindBestColoredPixel(image, out debugBest);
            Bgr bgr = image[bestpos];

            #region classification
            CardColor classification = classifier.Classify(bgr);
            CardColor verdict = classification;
            if (verdict == CardColor.Other)
            {
                if (!isGray(bgr, 10))
                {
                    verdict = ClassifyBgr2(bgr);
                }
                else
                {
                    verdict = CardColor.White;
                }
            } 
            #endregion
            #if DEBUG
            //save for training:
            writer.Write((int)bgr.Blue, (int)bgr.Green, (int)bgr.Red);
            #endif

            #region debug
            if (settings.debuglevel >= 4)
            {
                ImageViewer.Show(debugBest, verdict.ToString());
            }
            else if (settings.debuglevel >= 2 && verdict == CardColor.Other)
            {
                ImageViewer.Show(debugBest, verdict.ToString());
            } 
            #endregion
            return verdict;
        }

        public static Point FindBestColoredPixel(Image<Bgr, Byte> image, out Image<Bgr, Byte> debug)
        {
            //Find the pixel where the color is the least value (high value = white) and the most saturated
            Image<Hsv, Byte> hsvImage = image.Convert<Hsv, Byte>();

            double[] mins, maxs;
            Point[] minlocs, maxlocs;
            Image<Gray, Byte>[] channels = hsvImage.Split();
            Image<Gray, Byte> hChannel = channels[0];
            Image<Gray, Byte> sChannel = channels[1];
            Image<Gray, Byte> vChannel = channels[2];

            vChannel._Not();
            Image<Gray, Byte> use = vChannel.Mul(sChannel, 0.015);

            use.MinMax(out mins, out maxs, out minlocs, out maxlocs);

            Point max = maxlocs[0];

            #region debug
            Bgr col = image[max];
            debug = image.Clone(); //Image<Bgr, Byte> 
            debug.Draw(new CircleF(new PointF(max.X, max.Y), 5), new Bgr(255, 255, 255), 1);
            debug.Draw(new CircleF(new PointF(max.X, max.Y), 6), col, 2);
            debug.Draw(new CircleF(new PointF(max.X, max.Y), 8), new Bgr(255, 255, 255), 1);
            //ImageViewer.Show(debug);
            #endregion

            return max;
        }

        public static CardColor FindMostOccuringColor(Image<Bgr, Byte> image, int step, params CardColor[] ignore)
        {
            Bgr black = new Bgr(0,0,0);

            List<CardColor> colors = new List<CardColor>(image.Size.Height * image.Size.Width);
            for (int x = 0; x < image.Size.Width; x++)
            {
                for (int y= 0; y < image.Size.Height; y++)
                {
                    Bgr bgr = image[y, x];
                    if (!bgr.Equals(black))
                    {
                        CardColor col = classifier.Classify(bgr);
                        if (!ignore.Contains(col))
                        {
                            colors.Add(col);
                        } 
                    }
                }
            }
            CardColor most = (from item in colors 
                              group item by item into g 
                              orderby g.Count() descending 
                              select g.Key)
                              .First();
            return most;
        }
        #endregion
        #endregion

        #region fill
        private static void AssignFills(ContourNode tree)
        {
            AssignFill(tree);
            foreach (ContourNode child in tree.Children)
            {
                AssignFills(child);
            }
        }

        private static void AssignFill(ContourNode node)
        {
            if (node.Shape == Shape.Card)
            {
                node.Fill = DetermineFill(node);
            }
        }

        private static Fill DetermineFill(ContourNode cardNode)
        {
            Fill fill = Fill.Other;

            try
            {
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
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new VisionException("Could not distinguish shape from card", e, cardNode.Image);
            }
            return fill;
        }

        private static bool isDashed(ContourNode inner)
        {
            Image<Bgr, Byte> im = inner.Image.Clone();
            Rectangle oldroi = inner.Image.ROI;

            #region calc ROI
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

        #region filter
        private static void FilterTree(ContourNode tree)
        {
            FilterNode(tree);
            foreach (ContourNode child in tree.Children)
            {
                //FilterNode(child);
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
        #endregion
    }
}
