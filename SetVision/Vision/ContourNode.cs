using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using System.Drawing;
using SetVision.Gamelogic;
using Emgu.CV.Structure;

namespace SetVision.Vision
{
    public class ContourNode
    {
        public List<ContourNode> Children;
        public Contour<Point> Contour;
        public ContourNode Parent;

        public Fill Fill;
        public Shape Shape;
        public CardColor Color;

        private Image<Bgr, Byte> _image;
        public Image<Bgr, Byte> Image
        {
            get
            {
                if(_image==null)
                {
                    return null;
                    //throw new InvalidOperationException("Image is not yet initialized");
                }
                else
                {
                    return _image;
                }
            }
            set
            {
                _image = value;
            }
        }
        public Image<Gray, Byte> AttentionMask;

        public Bgr averageBgr;
        public Hsv averageHsv;

        public ContourNode(Contour<Point> node)
        {
            this.Contour = node;
            
            Children = new List<ContourNode>();
            List<Contour<Point>> kids = new List<Contour<Point>>(ContourNode.GetChildren(this.Contour));
            foreach(Contour<Point> kid in kids)
            {
                ContourNode kidnode = new ContourNode(kid);
                kidnode.Parent = this;
                Children.Add(kidnode);
            }
        }

        #region helpers
        public static IEnumerable<Contour<Point>> GetSiblings(Contour<Point> cont)
        {
            for (Contour<Point> _cont = cont;
                _cont != null;
                _cont = _cont.HNext)
            {
                yield return _cont;
            }
        }

        public static IEnumerable<Contour<Point>> GetChildren(Contour<Point> cont)
        {
            for (Contour<Point> _cont = cont;
                _cont != null;
                _cont = _cont.HNext)
            {
                Contour<Point> child = _cont.VNext;
                if (child != null)
                {
                    //yield return _cont.VNext;
                    foreach(Contour<Point> sibling_of_child in GetSiblings(child))
                    {
                        yield return sibling_of_child;
                    }
                }
            }
        }

        public ContourNode FindParent(Shape? shape, Fill? fill, CardColor? color)
        {
            ContourNode parent = this.Parent;
            while (parent != null)
            {
                bool shapeOK = (parent.Shape == shape) || (shape == null);
                bool fillOK = (parent.Fill == fill) || (fill == null);
                bool colorOK = (parent.Color == color) || (color == null);

                if (shapeOK && fillOK && colorOK)
                {
                    return parent;
                }
                else
                {
                    parent = parent.Parent;
                }
            }
            return null;
        }
        #endregion

        public Image<Bgr, Byte> ExtractContourTreeImages(Image<Bgr, Byte> source)
        {
            foreach (ContourNode child in Children)
            {
                ExtractContourTreeImages(source);
            }
            return ExtractImage(source);
        }

        private Image<Bgr, Byte> ExtractImage(Image<Bgr, Byte> source)
        {
            Image = ExtractContourImage(source, Contour, out AttentionMask);
            Image.ROI = Contour.GetMinAreaRect().MinAreaRect();
            AttentionMask.ROI = Image.ROI;
            return Image;
        }
        private Image<Bgr, Byte> ExtractContourImage(Image<Bgr, Byte> source, Contour<Point> contour, out Image<Gray, Byte> mask)
        {
            mask = source.Convert<Gray, Byte>();
            mask.SetZero();
            //Contour<Point> shifted = ShiftContour(contour, -3,-3);
            mask.Draw(contour, new Gray(255), new Gray(0), 2, -1);

            return source.And(new Bgr(255, 255, 255), mask);
        }
    }
}
