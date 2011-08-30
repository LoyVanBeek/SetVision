using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using System.Drawing;

namespace SetVision.Vision
{
    public class FeatureCardDetector : ICardDetector
    {
        public Image<Gray, Byte> Run(int hessianTresh, bool extended, 
            int neighbours, int emax, 
            double uniquenessThreshold,
            double scaleIncrement, int rotBins, 
            bool show)
        {
            SURFDetector detector = new SURFDetector(hessianTresh, extended); //hessianThresh=500, extended=false

            Image<Bgr, Byte> modelImageBgr = new Image<Bgr, byte>(@"images\640x480\3_purple_oval_full_cropped.bmp");//.Convert<Gray, Byte>();
            Image<Gray, Byte> modelImage = modelImageBgr.Convert<Gray, Byte>();
            //extract features from the object image
            SURFFeature[] modelFeatures = modelImage.ExtractSURF(ref detector);

            Image<Gray, Byte> observedImage = new Image<Gray, byte>(@"images\640x480\scene1.png");
            // extract features from the observed image
            SURFFeature[] imageFeatures = observedImage.ExtractSURF(ref detector);

            //Create a SURF Tracker using k-d Tree
            SURFTracker tracker = new SURFTracker(modelFeatures);
            //Comment out above and uncomment below if you wish to use spill-tree instead
            //SURFTracker tracker = new SURFTracker(modelFeatures, 50, .7, .1);

            SURFTracker.MatchedSURFFeature[] matchedFeatures = tracker.MatchFeature(imageFeatures, neighbours, emax); //neighbours=2, emax=20
            matchedFeatures = SURFTracker.VoteForUniqueness(matchedFeatures, uniquenessThreshold);//uniquenessThreshold=0.8
            matchedFeatures = SURFTracker.VoteForSizeAndOrientation(matchedFeatures, scaleIncrement, rotBins); //scaleIncrement=1.5, rotBins=20
            HomographyMatrix homography = SURFTracker.GetHomographyMatrixFromMatchedFeatures(matchedFeatures);

            //Merge the object image and the observed image into one image for display
            Image<Gray, Byte> res = modelImage.ConcateHorizontal(observedImage);

            #region draw lines between the matched features
            foreach (SURFTracker.MatchedSURFFeature matchedFeature in matchedFeatures)
            {
                PointF p = matchedFeature.ObservedFeature.Point.pt;
                p.X += modelImage.Width;
                res.Draw(new LineSegment2DF(matchedFeature.SimilarFeatures[0].Feature.Point.pt, p), new Gray(0), 1);
            }
            #endregion

            #region draw the project region on the image
            if (homography != null)
            {  //draw a rectangle along the projected model
                Rectangle rect = modelImage.ROI;
                PointF[] pts = new PointF[] { 
               new PointF(rect.Left, rect.Bottom),
               new PointF(rect.Right, rect.Bottom),
               new PointF(rect.Right, rect.Top),
               new PointF(rect.Left, rect.Top)};
                homography.ProjectPoints(pts);

                for (int i = 0; i < pts.Length; i++)
                    pts[i].Y += modelImage.Height;

                res.DrawPolyline(Array.ConvertAll<PointF, Point>(pts, Point.Round), true, new Gray(255.0), 5);
            }
            #endregion

            if (show)
            {
                ImageViewer.Show(res);
            }

            return res;
        }

        #region ICardDetector Members

        public IDictionary<SetVision.Gamelogic.Card, Point> LocateCards(Image<Bgr, byte> table)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
