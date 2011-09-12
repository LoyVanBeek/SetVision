using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SetVision.Vision
{
    public class InvalidCardException : SetGameException
    {
        public readonly string Property;
        public readonly object Value;

        public InvalidCardException(string property, object value)
            : base("Trying to create card with invalid arguments")
        {
            Property = property;
            Value = value;
        }
    }
}
