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
        public IDictionary<Card, Point> LocateCards(Image<Bgr, Byte> table)
        {
            #region process image
            //Convert the image to grayscale and filter out the noise
            Image<Gray, Byte> gray = table.Convert<Gray, Byte>();

            Gray cannyThreshold = new Gray(180);
            Gray cannyThresholdLinking = new Gray(120);
            Gray circleAccumulatorThreshold = new Gray(120);

            Image<Gray, Byte> cannyEdges = gray.Canny(cannyThreshold, cannyThresholdLinking);
            
            StructuringElementEx el = new StructuringElementEx(3,3, 2,2,CV_ELEMENT_SHAPE.CV_SHAPE_RECT);
            cannyEdges = cannyEdges.MorphologyEx(el, CV_MORPH_OP.CV_MOP_CLOSE, 1);
            #endregion

            #region find contours list
            //List<List<LineSegment2D>> contourPolys = new List<List<LineSegment2D>>(); //but this is very good

            //using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
            //{
            //    for (Contour<Point> contours = cannyEdges.FindContours(); contours != null; contours = contours.HNext)
            //    {
            //        Contour<Point> currentContour = contours.ApproxPoly(contours.Perimeter * 0.000, storage); //Maybe tune the accuracy?

            //        #region filter contours
            //        if (contours.Area > 250) //only consider contours with area greater than 250
            //        {
            //            #region stuff by loy
            //            Point[] points = currentContour.ToArray();
            //            List<LineSegment2D> contourEdges = new List<LineSegment2D>(PointCollection.PolyLine(points, true));
            //            contourPolys.Add(contourEdges);

            //            #endregion stuff by loy
            //        }
            //        #endregion filter contours
            //    }
            //}
            #endregion

            Contour<Point> contours = cannyEdges.FindContours(
                CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
                RETR_TYPE.CV_RETR_TREE);

            ContourNode tree = new ContourNode(contours);
            LabelTree(tree);

            StripDoubles(tree);

            ColorizeTree(tree, table);

            #region draw

            TreeViz.VizualizeTree(tree);
            DrawContours(tree, table);
            ImageViewer.Show(table);
            #endregion
            return new Dictionary<Card, Point>();
        }

        #region labeling
        private void LabelTree(ContourNode tree)
        {
            LabelNode(tree);
            foreach (ContourNode child in tree.Children)
            {
                LabelNode(child);
                LabelTree(child); //TODO: should be enabled
            }
        }
        private void LabelNode(ContourNode node)
        {
            if (IsCard(node))
            {
                node.Label = "Card";
            }
            else if (IsSquiggle(node))
            {
                node.Label = "Squiggle";
            }
            else if (IsOval(node))
            {
                node.Label = "Oval";
            }
            else if (IsDiamond(node))
            {
                node.Label = "Diamond";
            }
            else
            {
                node.Label = "_";
            }
        }
        #endregion

        #region actual labels
        private bool IsDiamond(ContourNode node)
        {
            bool inCard = node.Parent != null &&
                   (node.Parent.Label == "Card" ||
                       node.Parent.Label == "Diamond");
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
                   (node.Parent.Label == "Card" ||
                       node.Parent.Label == "Oval");
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
                (node.Parent.Label == "Card" ||
                    node.Parent.Label == "Squiggle");
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
        #endregion

        #region drawing
        private void DrawContours(ContourNode node, Image<Bgr, Byte> canvas, System.Drawing.Color color)
        {
            Bgr _color = new Bgr(System.Drawing.Color.Red);

            foreach (ContourNode child in node.Children)
            {
                canvas.DrawPolyline(child.Contour.ToArray(), true, _color, 1);
                
                if (!String.IsNullOrEmpty(node.Label))
	            {
                    MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_PLAIN, 1,1);
                    canvas.Draw(child.Label,
                        ref font,
                        child.Contour[0],
                        new Bgr(System.Drawing.Color.Red)
                        );
	            }

                DrawContours(child, canvas, color);
            }
        }

        private void DrawContours(ContourNode node, Image<Bgr, Byte> canvas)
        {
            #region color
            Random rnd = new Random(node.Contour.Ptr.ToInt32());
            Bgr _color = new Bgr(rnd.Next(255), rnd.Next(255), rnd.Next(255));
            #endregion

            foreach (ContourNode child in node.Children)
            {
                canvas.DrawPolyline(child.Contour.ToArray(), true, _color, 1);

                if (!String.IsNullOrEmpty(node.Label))
                {
                    MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_PLAIN, 1, 1);
                    canvas.Draw(child.Label,
                        ref font,
                        child.Contour[0],
                        new Bgr(System.Drawing.Color.Red)
                        );
                }

                DrawContours(child, canvas);
            }
        }

        private void ShowContour(Contour<Point> contour, Image<Bgr, Byte> original_image, string message)
        {
            Image<Bgr, Byte> canvas = original_image.Clone();
            canvas.SetZero();

            canvas.DrawPolyline(contour.ToArray(), true, new Bgr(255, 255, 255), 1);
            ImageViewer.Show(canvas, message);
        }
        #endregion

        #region color
        private void ColorizeTree(ContourNode tree, Image<Bgr, Byte> image)
        {
            ColorizeNode(tree, image);
            foreach (ContourNode child in tree.Children)
            {
                ColorizeNode(child, image);
                ColorizeTree(child, image);
            }
        }
        private void ColorizeNode(ContourNode node, Image<Bgr, Byte> image)
        {
            if (!String.IsNullOrEmpty(node.Label)
                && node.Contour.Area > 100
                && ((node.Label == "Squiggle")
                    || (node.Label == "Diamond")
                    || (node.Label == "Oval")))
            {
                CardColor color = RecognizeColor(image, node);
                node.Color = color;
            }
        }

        private CardColor RecognizeColor(Image<Bgr, Byte> _image, ContourNode node)
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
            Image<Bgr, Byte> image = _image;
            Image<Gray, Byte> mask = image.Convert<Gray, Byte>();
            mask.SetZero();
            mask.Draw(contour, new Gray(255), new Gray(0), 2, -1);

            mask.Erode(1);

            Image<Bgr, Byte> rgbMask = new Image<Bgr, byte>(new Image<Gray, byte>[] { mask, mask, mask });

            Image<Bgr, Byte> focusBgr = image.And(new Bgr(255, 255, 255), mask);
            Image<Hsv, Byte> focusHsv = image.And(new Bgr(255, 255, 255), mask).Convert<Hsv, Byte>();
            Bgr avgBgr = new Bgr();
            MCvScalar scr1 = new MCvScalar();
            focusBgr.AvgSdv(out avgBgr, out scr1, mask);

            Hsv avgHsv = new Hsv();
            MCvScalar scr2 = new MCvScalar();
            focusHsv.AvgSdv(out avgHsv, out scr2, mask);

            #endregion

            CardColor color = CardColor.White;
            #region classify
            if(avgHsv.Satuation < 30)
            {
                color = CardColor.White;
            }
            else if (avgBgr.Red > avgBgr.Blue && avgBgr.Red > avgBgr.Green)
            {
                color = CardColor.Red;
            }
            else if (avgBgr.Green > avgBgr.Blue && avgBgr.Green > avgBgr.Red)
            {
                color = CardColor.Green;
            }
            else if (avgBgr.Green < avgBgr.Blue && avgBgr.Green < avgBgr.Red)
            {
                color = CardColor.Purple;
            }
            //else if (avgHsv.Satuation < 50 && avgHsv.Satuation > 30)
            //{
            //    fill = Fill.Dashed;
            //}
            else
            {
                color = CardColor.White;
                //throw new Exception("Could not determine colors of the contour");
            }
            #endregion

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
            //debug = debug.ConcateHorizontal(rgbMask);
            //ImageViewer.Show(debug, "rgb(" + bgrstr + ") hsv(" + hsvstr + ")"+"Classified as "+color.ToString());
            #endif
            #endregion

            focusBgr.ROI = contour.GetMinAreaRect().MinAreaRect();
            node.image = focusBgr;

            return color;
        }
        #endregion

        /// <summary>
        /// Removes nodes with the same label and area from the tree, stripping the node out the tree and 
        /// connecting its children to its parent (A-B-C) to (A-C)
        /// </summary>
        /// <param name="tree"></param>
        public void StripDoubles(ContourNode tree)
        {
            if (tree.Parent != null)
            {
                if ((tree.Parent.Label == tree.Label)
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
        /* TODO: make a function that prunes the tree of ContourNodes. 
         * It strips away contours with (about) the same area as their parents, and makes its own parent the parent of its children (A-B-C to A-C)
         * 
         * Then, if a node has a color and no children, its Solid
         * if it has 1 child with Card-color, its Open. 
         * If there is a child with the same color, it Dashed.
         */
    }
}
