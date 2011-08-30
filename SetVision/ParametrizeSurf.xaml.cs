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
using System.Windows.Shapes;
using SetVision.Vision;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Runtime.InteropServices;

namespace SetVision
{
    /// <summary>
    /// Interaction logic for ParametrizeSurf.xaml
    /// </summary>
    public partial class ParametrizeSurf : Window
    {
        FeatureCardDetector detector;

        public ParametrizeSurf()
        {
            InitializeComponent();

            detector = new FeatureCardDetector();

            //slider_Emax.MouseLeftButtonUp                   += new MouseButtonEventHandler(slider_MouseLeftButtonUp);
            //slider_hessianThres.MouseLeftButtonUp           += new MouseButtonEventHandler(slider_MouseLeftButtonUp);
            //slider_Neighbours.MouseLeftButtonUp             += new MouseButtonEventHandler(slider_MouseLeftButtonUp);
            //slider_RotBins.MouseLeftButtonUp                += new MouseButtonEventHandler(slider_MouseLeftButtonUp);
            //slider_ScaleIncrement.MouseLeftButtonUp         += new MouseButtonEventHandler(slider_MouseLeftButtonUp);
            //slider_UniquenessThreshold.MouseLeftButtonUp    += new MouseButtonEventHandler(slider_MouseLeftButtonUp);
            
            //toggle_extended.Click += new RoutedEventHandler(toggle_extended_Click);
        }

        void slider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Update();
        }

        void toggle_extended_Click(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private void setter_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Update();
        }

        private void Update()
        {
            this.Title = this.Title.Replace("- Error, retry", "");
            
            try
            {
                Image<Gray, Byte> result = detector.Run(
                        (int)slider_hessianThres.Value,
                        toggle_extended.IsChecked.Value,
                        (int)slider_Neighbours.Value,
                        (int)slider_Emax.Value,
                        slider_UniquenessThreshold.Value,
                        slider_ScaleIncrement.Value,
                        (int)slider_RotBins.Value,
                        false);

                resultImage.Source = ToBitmapSource(result);
            }
            catch (Exception)
            {
                this.Title = this.Title + "- Error, retry";
            }
        }
        
        /// <summary>
        /// Delete a GDI object
        /// </summary>
        /// <param name="o">The poniter to the GDI object to be deleted</param>
        /// <returns></returns>
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);
        /// <summary>
        /// Convert an IImage to a WPF BitmapSource. The result can be used in the Set Property of Image.Source
        /// </summary>
        /// <param name="image">The Emgu CV Image</param>
        /// <returns>The equivalent BitmapSource</returns>
        public static BitmapSource ToBitmapSource(IImage image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }

        private void button_Update_Click(object sender, RoutedEventArgs e)
        {
            Update();
        }
    }
}
