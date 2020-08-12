using PLCConnector;
using PLCConnector.L2;
using System;
using System.Collections.Generic;
using System.Text;

namespace LineMap.Messages.SA
{
    public class Message2013 : GenericL2Message
    {

        public Message2013(DataBlock other) : base(other)
        {

        }

        public int ORDER_ID => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 0].As<int>();

        public int MISSION_ID => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 1].As<int>();

        public int DEVICE_NR => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 2].As<int>();

        public int MISSION_TYPE => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 3].As<int>();

        public int TRACK_ID => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 4].As<int>();

        public int DEVICE => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 5].As<int>();

        public int SIDE => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 6].As<int>();

        public int LEVEL => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 7].As<int>();

        public int POSITION => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 8].As<int>();

        public int STEP_RESULT => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 9].As<int>();

        public int MISSION_RESULT => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 10].As<int>();

        public int BARCODE => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 11].As<int>();

    }
}
