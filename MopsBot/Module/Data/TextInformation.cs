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
            #if NET40
                StreamReader read = new StreamReader("data//quotes.txt");
            #else
                StreamReader read = new StreamReader(new FileStream("data//quotes.txt",FileMode.Open));
            #endif
            cookies = int.Parse(read.ReadLine());

            int count = int.Parse(read.ReadLine());

            string quoteAll = read.ReadToEnd();

            string[] splitQuote = quoteAll.Split('\0');

            foreach(string aquote in splitQuote)
            {
                quotes.Add(aquote.Trim('\r','\n'));
            }

            #if NET40
                read.Close();
            #else
                read.Dispose();
            #endif
        }

        public void writeInformation()
        {
            #if NET40
                StreamWriter write = new StreamWriter("data//quotes.txt");
            #else
                StreamWriter write = new StreamWriter(new FileStream("data//quotes.txt",FileMode.OpenOrCreate));
            #endif

            write.WriteLine(cookies);
            write.WriteLine(quotes.Count);

                string output = string.Join("\0", quotes.ToArray());
                write.Write(output);
            
            #if NET40
                write.Close();
            #else  
                write.Dispose();
            #endif
        }
    }
}
