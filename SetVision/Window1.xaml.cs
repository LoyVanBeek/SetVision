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
            this.Title = "Play Set with a photo";
        }

        private Image<Bgr, Byte> Run(string filename)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            ContourAnalyzer ca = new ContourAnalyzer();
            Image<Bgr, Byte> table = new Image<Bgr, byte>(filename);
            table = table.PyrDown().PyrDown();//.PyrDown().PyrUp();
            Dictionary<Card, System.Drawing.Point> cards = ca.LocateCards(table);

            Logic logic = new Logic();
            HashSet<List<Card>> sets = logic.FindSets(new List<Card>(cards.Keys));

            Random rnd = new Random();
            foreach (List<Card> set in sets)
            {
                DrawSet(table, cards, rnd, set);
            }

            watch.Stop();
            this.Title = String.Format("Done. Elapsed time: {0}", watch.Elapsed.ToString());

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
            var table = Run(@"images/scene4.jpg");
            ImageViewer.Show(table); 
        }
    }
}
