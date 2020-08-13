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

        public int MSG_LEN
        {
            get { return this[0].As<int>(); }
            set { this[0].Value = value; }
        }

        public int PR_MSG
        {
            get { return this[1].As<int>(); }
            set { this[1].Value = value; }
        }

        public int ID_MSG
        {
            get { return this[2].As<int>(); }
            set { this[2].Value = value; }
        }

        public int ID_PLC
        {
            get { return this[3].As<int>(); }
            set { this[3].Value = value; }
        }

        public int ID_SRC
        {
            get { return this[4].As<int>(); }
            set { this[4].Value = value; }
        }

        public int FOOTER
        {
            get { return this[this.Fields.Count - 1].As<int>(); }
            set { this[this.Fields.Count - 1].Value = value; }
        }

    }
}
