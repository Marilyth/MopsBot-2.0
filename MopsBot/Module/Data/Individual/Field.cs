using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Module.Data.Individual
{
    class Field
    {
        internal char letter { get; set; }
        internal Type fieldType { get; set; }

        internal enum Type { belongs, empty };

        public Field(Type type)
        {
            fieldType = type;
        }

        public void setChar(char pLetter)
        {
            letter = pLetter;
            fieldType = Type.belongs;
        }
    }
}
