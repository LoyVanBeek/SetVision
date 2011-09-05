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
        public Shape Shape;
        public CardColor Color;
        public Fill Fill;
        public Image<Bgr, Byte> image;
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
                bool shapeOK = (parent.Shape == shape)  || (shape==null);
                bool fillOK = (parent.Fill == fill)     || (fill == null);
                bool colorOK = (parent.Color == color)  || (color == null);

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
    }
}
