using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Emgu.CV.Structure;

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

        private IEnumerable<Brush> GenerateBrushes(int stepsize)
        {
            BrushConverter converter = new BrushConverter();
            for (int b = 0; b < 255; b += stepsize)
            {
                for (int g = 0; g < 255; g += stepsize)
                {
                    for (int r = 0; r < 255; r += stepsize)
                    {
                        yield return new SolidColorBrush(Color.FromScRgb(0, r, g, b));
                    }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //List<Brush> colors = new List<Brush>(GenerateBrushes(10));
            //ColorDisplay.ItemsSource = colors;
            foreach(Brush brush in GenerateBrushes(50))
            {
                ListViewItem item = new ListViewItem();
                item.Background = brush;
                ColorDisplay.Items.Add(item);
            }
            ColorDisplay.UpdateLayout();
        }
    }
}
