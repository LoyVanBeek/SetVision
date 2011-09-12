using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SetVision.Vision
{
    public class SetGameException : Exception
    {
        public SetGameException(string message)
            : base(message)
        {
        }
    }
}
