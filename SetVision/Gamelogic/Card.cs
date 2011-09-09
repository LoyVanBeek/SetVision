using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SetVision.Vision;

namespace SetVision.Gamelogic
{
    public enum CardColor
    {
        Purple,
        Green,
        Red,
        White,
        Other
    }

    public enum Shape
    {
        Oval,
        Diamond,
        Squiggle,
        Card,
        Other
    }

    public enum Fill
    {
        Open,
        Dashed,
        Solid,
        Other
    }

    public class Card
    {
        public CardColor Color { get; private set; }
        public Shape Shape { get; private set; }
        public Fill Fill { get; private set; }
        public int Count { get; private set; }

        public Card(CardColor color, Shape shape, Fill fill, int count)
        {
            #region validation
            if (color == CardColor.Other)
            {
                throw new InvalidCardException("color", color);
            } 
            if (shape == Shape.Other)
            {
                throw new InvalidCardException("shape", shape);
            }
            if (fill == Fill.Other)
            {
                throw new InvalidCardException("fill", fill);
            }
            if (count > 3 || count < 1)
            {
                throw new InvalidCardException("count", count);
            }
            #endregion validation

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
