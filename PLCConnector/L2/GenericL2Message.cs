using System;
using System.Collections.Generic;
using System.Text;

namespace PLCConnector.L2
{
    public class GenericL2Message : DataBlock
    {

        public GenericL2Message(DataBlock other) : base(other)
        {

        }

        public int MSG_LEN => this[0].As<int>();

        public int PR_MSG => this[1].As<int>();

        public int ID_MSG => this[2].As<int>();

        public int ID_PLC => this[3].As<int>();

        public int ID_SRC => this[4].As<int>();

        public int FOOTER => this[Fields.Count - 1].As<int>();

    }
}
