using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Features2D;
using System.Diagnostics;

namespace SURFFeatureExample
{
   static class Program
   {
      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      [STAThread]
      static void Main()
      {
         Application.EnableVisualStyles();
         Application.SetCompatibleTextRenderingDefault(false);
         Run();
      }

      static void Run()
      {
         SURFDetector surfParam = new SURFDetector(500, false);

         Image<Gray, Byte> modelImage = new Image<Gray, byte>("box.png");
         //extract features from the object image
         ImageFeature[] modelFeatures = surfParam.DetectFeatures(modelImage, null);

         //Create a Feature Tracker
         Features2DTracker tracker = new Features2DTracker(modelFeatures);

         Image<Gray, Byte> observedImage = new Image<Gray, byte>("box_in_scene.png");

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

         ImageViewer.Show(res, String.Format("Matched in {0} milliseconds", watch.ElapsedMilliseconds));
      }
   }
}
