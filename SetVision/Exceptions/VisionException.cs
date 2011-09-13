using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;

namespace SetVision.Exceptions
{
    public class VisionException : Exception
    {
        public IImage Image { get; private set; }

        public VisionException(string message, Exception inner, IImage image)
            : base(message, inner)
        {
            Image = image;
        }
    }
}
