using PLCConnector;
using PLCConnector.L2;
using System;
using System.Collections.Generic;
using System.Text;

namespace LineMap.Messages
{
    public class Message1006 : GenericL2Message
    {

        public Message1006(DataBlock other) : base(other)
        {

        }

        public int LINE_NR => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 0].As<int>();

        public int POSITION_NR => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 1].As<int>();

        public int DOFF_NR => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 2].As<int>();

    }
}
