using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SetVision.Learning
{
    public class CsvWriter : IDisposable
    {
        StringBuilder sb;
        StreamWriter writer;

        public CsvWriter(string filename)
        {
            writer = new StreamWriter(filename, true);
            sb = new StringBuilder();
            writer.AutoFlush = true;
        }

        public void Close()
        {
            writer.Close();
        }

        public void Write(params object[] data)
        {
            foreach (object obj in data)
            {
                writer.Write(obj.ToString() + ";");
            }
            writer.WriteLine();
        }

        #region IDisposable Members

        public void Dispose()
        {
            Close();
        }

        #endregion
    }
}
