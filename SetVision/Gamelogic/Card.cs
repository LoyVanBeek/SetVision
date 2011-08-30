using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SetVision.Gamelogic
{
    public enum Color
    {
        Purple,
        Green,
        Red
    }

    public enum Shape
    {
        Oval,
        Diamond,
        Squiggle
    }

    public enum Fill
    {
        Open,
        Dashed,
        Solid
    }

    public class Card
    {
        public Color Color { get; private set; }
        public Shape Shape { get; private set; }
        public Fill Fill { get; private set; }
        public int Count { get; private set; }

        public Card(Color color, Shape shape, Fill fill, int count)
        {
            this.Color = color;
            this.Shape = shape;
            this.Fill = fill;
            this.Count = count;
        }

        public string this[string property]
        {
            get
            {
                switch (property.ToLower())
                {
                    case "color":
                        return this.Color.ToString();
                    case "shape":
                        return this.Shape.ToString();
                    case "fill":
                        return this.Fill.ToString();
                    case "count":
                        return this.Count.ToString();
                    default:
                        throw new ArgumentException("'"+property+"' is not a property of card");
                }
            }
        }
    }
}
