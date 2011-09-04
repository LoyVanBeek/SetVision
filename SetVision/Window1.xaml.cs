using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using SetVision.Vision;
using SetVision.Gamelogic;
using System.Collections.Generic;

namespace SetVision
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(Window1_Loaded);
        }

        void Window1_Loaded(object sender, RoutedEventArgs e)
        {
            ContourAnalyzer ca = new ContourAnalyzer();
            Image<Bgr, Byte> table = new Image<Bgr, byte>(@"images/scene3.jpg");
            table = table.PyrDown().PyrDown();
            Dictionary<Card, System.Drawing.Point> cards = ca.LocateCards(table);

            Logic logic = new Logic();
            HashSet<List<Card>> sets = logic.FindSets(new List<Card>(cards.Keys));
            
            Random rnd = new Random();
            foreach (List<Card> set in sets)
            {
                Bgr setcolor = new Bgr(rnd.Next(255), rnd.Next(255), rnd.Next(255));

                foreach (Card card in cards.Keys)
                {
                    System.Drawing.Point p = cards[card];
                    CircleF circle = new CircleF(new PointF(p.X, p.Y), 50);
                    table.Draw(circle, setcolor, 2);
                }
            }
            
            
            ImageViewer.Show(table);
        }

        static void Run()
        {
            SURFDetector surfParam = new SURFDetector(500, false);

            Image<Gray, Byte> modelImage = new Image<Gray, byte>(@"resources\raw0.jpg");
            modelImage = modelImage.PyrDown().PyrDown();
            
            //extract features from the object image
            ImageFeature[] modelFeatures = null;
            try
            {
                modelFeatures = surfParam.DetectFeatures(modelImage, null);
            }
            catch (AccessViolationException ave)
            {
                //This can happen when no features were detected...
                MessageBox.Show("Possibly, there were no features detected:"+Environment.NewLine+ave.Message);
            }

            if (modelFeatures != null)
            {
                //Create a Feature Tracker
                Features2DTracker tracker = new Features2DTracker(modelFeatures);

                Image<Gray, Byte> observedImage = new Image<Gray, byte>(@"resources\scene2.jpg");
                observedImage = observedImage.PyrDown().PyrDown();

                Stopwatch watch = Stopwatch.StartNew();
                // extract features from the observed image
                ImageFeature[] imageFeatures = surfParam.DetectFeatures(observedImage, null);

                Features2DTracker.MatchedImageFeature[] matchedFeatures = tracker.MatchFeature(imageFeatures, 2, 20);
                matchedFeatures = Features2DTracker.VoteForUniqueness(matchedFeatures, 0.8);
                matchedFeatures = Features2DTracker.VoteForSizeAndOrientation(matchedFeatures, 1.5, 20);
                HomographyMatrix homography = Features2DTracker.GetHomographyMatrixFromMatchedFeatures(matchedFeatures);
                watch.Stop();

                //Merge the object image and the observed image into one image for display
                Image<Gray, Byte> res = modelImage.ConcateVertical(observedImage);

                #region draw lines between the matched features
                foreach (Features2DTracker.MatchedImageFeature matchedFeature in matchedFeatures)
                {
                    PointF p = matchedFeature.ObservedFeature.KeyPoint.Point;
                    p.Y += modelImage.Height;
                    res.Draw(new LineSegment2DF(matchedFeature.SimilarFeatures[0].Feature.KeyPoint.Point, p), new Gray(0), 1);
                }
                #endregion

                #region draw the project region on the image
                if (homography != null)
                {  //draw a rectangle along the projected model
                    System.Drawing.Rectangle rect = modelImage.ROI;
                    PointF[] pts = new PointF[] { 
               new PointF(rect.Left, rect.Bottom),
               new PointF(rect.Right, rect.Bottom),
               new PointF(rect.Right, rect.Top),
               new PointF(rect.Left, rect.Top)};
                    homography.ProjectPoints(pts);

                    for (int i = 0; i < pts.Length; i++)
                        pts[i].Y += modelImage.Height;

                    res.DrawPolyline(Array.ConvertAll<PointF, System.Drawing.Point>(pts, System.Drawing.Point.Round), true, new Gray(255.0), 5);
                }
                #endregion

                ImageViewer.Show(res, String.Format("Matched in {0} milliseconds", watch.ElapsedMilliseconds)); 
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Run();
            FeatureCardDetector detector = new FeatureCardDetector();
            detector.Run(500, false, 2, 20, 0.8, 1.5, 20, true);
        }
    }
}
