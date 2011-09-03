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
using System.ComponentModel;
using System.Runtime.InteropServices;
using Emgu.CV;

namespace SetVision
{
    /// <summary>
    /// Interaction logic for TreeViz.xaml
    /// </summary>
    public partial class TreeViz : Window, INotifyPropertyChanged
    {
        private IEnumerable<ContourNode> _children;
        public IEnumerable<ContourNode> Children
        {
            get
            {
                return _children;
            }
            set
            {
                _children = value;
                NotifyPropertyChanged("Children");
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public TreeViz()
        {
            InitializeComponent();
        }
        public void FillTree(ContourNode tree)
        {
            List<ContourNode> _children = new List<ContourNode>();
            Children = _children;
        }

        public void Populate(ContourNode tree)
        {
            TreeViewItem item = new TreeViewItem();
            item.Header = tree.Label + ":" + tree.Color.ToString();
            ContourTree.Items.Add(item);

            foreach (ContourNode child in tree.Children)
            {
                Populate(child, item);
            }
        }

        private void Populate(ContourNode tree, TreeViewItem parentItem)
        {
            TreeViewItem item = new TreeViewItem();

            item.Header = tree.Label + ":" + tree.Color.ToString();
            Label lbl = new Label();
            lbl.Content = tree.Label + ":" + tree.Color.ToString();

            Image img = new Image();
            if (tree.image != null)
            {
                BitmapSource src = TreeViz.ToBitmapSource(tree.image);
                img.Source = src; 
            }

            StackPanel panel = new StackPanel();
            panel.Children.Add(lbl);
            panel.Children.Add(img);

            item.Header = panel;

            foreach (ContourNode child in tree.Children)
            {
                Populate(child, item);
            }

            parentItem.Items.Add(item);
        }

        public static void VizualizeTree(ContourNode tree)
        {
            TreeViz viz = new TreeViz();
            viz.Populate(tree); // viz.FillTree(tree);
            viz.ShowDialog();
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

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
    }
}
