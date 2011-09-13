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
using System.Windows.Forms;
using System.IO;
using System.Windows.Media;
using SetVision.Exceptions;

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
        }

        private Image<Bgr, Byte> Run(string filename)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            ContourAnalyzer ca = new ContourAnalyzer();
            Image<Bgr, Byte> table = new Image<Bgr, byte>(filename);
            table = table.PyrDown().PyrDown();

            int debuglevel = cmbDebuglevel.SelectedIndex+1; //The first item has index 0, but level 1

            Settings settings = new Settings(debuglevel);
            Dictionary<Card, System.Drawing.Point> cards = ca.LocateCards(table, settings);

            Logic logic = new Logic();
            HashSet<List<Card>> sets = logic.FindSets(new List<Card>(cards.Keys));

            Random rnd = new Random();
            foreach (List<Card> set in sets)
            {
                DrawSet(table, cards, rnd, set);
            }

            watch.Stop();
            this.Title = String.Format("Done. Elapsed time: {0}", watch.Elapsed.ToString());

            ImageSource bitmap = TreeViz.ToBitmapSource(table);
            Shower.Source = bitmap;

            return table;
        }

        private static void DrawSet(Image<Bgr, Byte> table, Dictionary<Card, System.Drawing.Point> cards, Random rnd, List<Card> set)
        {
            Bgr setcolor = new Bgr(rnd.Next(255), rnd.Next(255), rnd.Next(255));
            List<System.Drawing.Point> centers = new List<System.Drawing.Point>();

            foreach (Card card in set)
            {
                System.Drawing.Point p = cards[card];
                PointF center = new PointF(p.X, p.Y);
                centers.Add(p);
                CircleF circle = new CircleF(center, 50);
                table.Draw(circle, setcolor, 2);
            }

            table.DrawPolyline(centers.ToArray(), true, setcolor, 5);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //var table = Run(@"images/scene3.jpg");

            FileInfo file = new FileInfo(PathBox.Text);
            if(!file.Exists)
            {
                PathBox.Background = System.Windows.Media.Brushes.Red;
            }

            try
            {
                var table = Run(file.FullName);
                //ImageViewer.Show(table);
            }
            catch (SetGameException sge)
            {
                InvalidCardException ice = sge as InvalidCardException;
                if (ice != null)
                {
                    System.Windows.MessageBox.Show(
                        "Tried to create a card with an invalid property: " + ice.Property + "=" + ice.Value,
                        "Invalid card detected"
                        );
                }
            }
            catch (VisionException ve)
            {
                //System.Windows.MessageBox.Show("Something went wrong while analizing the image."+ 
                //"Are the cards separated, are the shapes not blurry and isn't the image underlit?");
                ImageViewer.Show(ve.Image, ve.Message+"Are the cards separated, are the shapes not blurry and isn't the image underlit?");
            }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            System.Windows.Forms.DialogResult result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string filename = dlg.FileName;
                FileInfo info = new FileInfo(filename);
                if (!info.Exists)
                {
                    RunButton.IsEnabled = false;
                }
                else
                {
                    RunButton.IsEnabled = true;
                    PathBox.Text = info.FullName;

                    try
                    {
                        Image<Bgr, Byte> table = new Image<Bgr, byte>(info.FullName);
                        ImageSource bitmap = TreeViz.ToBitmapSource(table.PyrDown().PyrDown());
                        Shower.Source = bitmap;
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                } 
            }
        }
    }
}
