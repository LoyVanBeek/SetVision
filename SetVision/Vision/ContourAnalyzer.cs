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
            Image<Gray, Byte> gray = table.Convert<Gray, Byte>().PyrDown().PyrDown();//.PyrUp(); //this makes the image blurry :-(

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

            #region draw
            table = table.PyrDown().PyrDown();

            DrawTree(tree, table);
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
            if (node.Parent != null && node.Parent.Label == "Card")
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
            if (node.Parent != null && node.Parent.Label == "Card")
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
            bool inCard = node.Parent != null && node.Parent.Label == "Card";
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
            bool area = node.Contour.Area > 25000;
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
            return area;
            
        }
        #endregion

        private void DrawTree(ContourNode node, Image<Bgr, Byte> canvas, System.Drawing.Color color)
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

                DrawTree(child, canvas, color);
            }
        }

        private void DrawTree(ContourNode node, Image<Bgr, Byte> canvas)
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

                DrawTree(child, canvas);
            }
        }

        private void ShowContour(Contour<Point> contour, Image<Bgr, Byte> original_image, string message)
        {
            Image<Bgr, Byte> canvas = original_image.Clone();
            canvas.SetZero();

            canvas.DrawPolyline(contour.ToArray(), true, new Bgr(255, 255, 255), 1);
            ImageViewer.Show(canvas, message);
        }
    }
}
