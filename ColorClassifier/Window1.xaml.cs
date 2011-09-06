using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Emgu.CV;
using Emgu.CV.ML;
using Emgu.CV.Structure;
using SetVision.Gamelogic;
using System.Drawing;
using SetVision.Vision;

namespace ColorClassifier
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

        private IEnumerable<Bgr> GenerateColors(int stepsize)
        {
            for (int b = 0; b < 255; b += stepsize)
            {
                for (int g = 0; g < 255; g += stepsize)
                {
                    for (int r = 0; r < 255; r += stepsize)
                    {
                        yield return new Bgr(b, g, r);
                    }
                }
            }
        }

        private IEnumerable<System.Windows.Media.Brush> GenerateBrushes(int stepsize)
        {
            BrushConverter converter = new BrushConverter();
            for (int b = 0; b < 255; b += stepsize)
            {
                for (int g = 0; g < 255; g += stepsize)
                {
                    for (int r = 0; r < 255; r += stepsize)
                    {
                        yield return new SolidColorBrush(System.Windows.Media.Color.FromScRgb(0, r, g, b));
                    }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            BgrHsvClassifier classifier = new BgrHsvClassifier();
            classifier.Train();
        }
    }
}
