using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SetVision.Gamelogic;
using Emgu.CV;
using System.Drawing;

namespace SetVision.Vision
{
    public class CardContour
    {
        public ContourNode Node { get; private set; }

        private Fill fill;
        new public Fill Fill
        {
            get
            {
                if (fill == null)
                {
                    fill = GetFill();
                }
                return fill;
            }
        }

        private Shape shape;
        new public Shape Shape
        {
            get
            {
                if (shape == null)
                {
                    shape = GetShape();
                }
                return shape;
            }
        }

        private CardColor color;
        new public CardColor Color
        {
            get
            {
                if (color == null)
                {
                    color = GetColor();
                }
                return color;
            }
        }

        private int count;
        public int Count
        {
            get
            {
                if (count == null)
                {
                    count = GetCount();
                }
                return count;
            }
        }

        private List<ShapeContour> children;
        public List<ShapeContour> Children
        {
            get
            {
                if (children == null)
                {
                    children = GetChildren();
                }
                return children;
            }
        }

        public CardContour(ContourNode node)
        {
            Node = node;
        }

        private List<ShapeContour> GetChildren()
        {
            List<ShapeContour> list = new List<ShapeContour>();
            foreach (ContourNode child in Node.Children)
            {
                if (child.Shape == Shape.Diamond    ||
                    child.Shape == Shape.Oval       ||
                    child.Shape == Shape.Squiggle)
                {
                    list.Add(new ShapeContour(child));
                }
            }

            return list;
        }

        public Card GetCard()
        {
            //foreach (ContourNode child in Children)
            //{

            //}

            Shape shape = GetShape();
            CardColor color = GetColor();
            Fill fill = GetFill();
            int count = GetCount();
            return new Card(color, shape, fill, count);
        }

        private Fill GetFill()
        {
            throw new NotImplementedException();
        }

        private CardColor GetColor()
        {
            throw new NotImplementedException();
        }

        private Shape GetShape()
        {
            var shapeCount =
                from shp in Children
                group shp by shp.Shape into g
                select new { g.Key, Count = g.Count() };

            int max = int.MinValue;
            Shape most = Shape.Other;
            foreach (var group in shapeCount)
            {
                if (group.Count > max)
                {
                    max = group.Count;
                    most = group.Key;
                }
            }
            return most;
        }

        private int GetCount()
        {
            return 0;
        }
    }
}
