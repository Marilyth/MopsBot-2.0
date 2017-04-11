using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Threading.Tasks;

namespace MopsBot.Module.Data
{
    class TextInformation
    {
        public List<String> quotes = new List<String>();
        public int cookies;

        public TextInformation()
        {
            StreamReader read = new StreamReader("data//quotes.txt");
            cookies = int.Parse(read.ReadLine());

            int count = int.Parse(read.ReadLine());

            string quoteAll = read.ReadToEnd();

            string[] splitQuote = quoteAll.Split('\0');

            foreach(string aquote in splitQuote)
            {
                quotes.Add(aquote.Trim('\r','\n'));
            }

            read.Close();
        }

        public void writeInformation()
        {
            StreamWriter write = new StreamWriter("data//quotes.txt");

            write.WriteLine(cookies);
            write.WriteLine(quotes.Count);

                string output = string.Join("\0", quotes.ToArray());
                write.Write(output);
            
            write.Close();
        }
    }
}
