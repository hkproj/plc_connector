using PLCConnector;
using PLCConnector.L2;
using System;
using System.Collections.Generic;
using System.Text;

namespace LineMap.Messages.SA
{
    public class Message2012 : GenericL2Message
    {

        public const int MESSAGE_ID = 2012;
        public const int MESSAGE_LENGTH = 20;
        public const int MESSAGE_DATA_FIELDS_NUM = MESSAGE_LENGTH - L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE - L2HandshakeProtocol.L2_MESASGE_FOOTER_SIZE;

        public Message2012(DataBlock other) : base(other)
        {
            this.ID_MSG = MESSAGE_ID;
            this.ID_SRC = 1;
            this.MSG_LEN = MESSAGE_LENGTH;
            this.FOOTER = MESSAGE_LENGTH;
        }

        public int ORDER_ID
        {
            get { return this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 0].As<int>(); }
            set { this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 0].Value = value; }
        }

        public int MISSION_ID
        {
            get { return this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 1].As<int>(); }
            set { this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 1].Value = value; }
        }

        public int DEVICE_NR
        {
            get { return this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 2].As<int>(); }
            set { this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 2].Value = value; }
        }

        public int MISSION_TYPE
        {
            get { return this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 3].As<int>(); }
            set { this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 3].Value = value; }
        }

        public int TRACK_ID
        {
            get { return this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 4].As<int>(); }
            set { this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 4].Value = value; }
        }

        public int SRC_DEVICE
        {
            get { return this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 5].As<int>(); }
            set { this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 5].Value = value; }
        }

        public int SRC_SIDE
        {
            get { return this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 6].As<int>(); }
            set { this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 6].Value = value; }
        }

        public int SRC_LEVEL
        {
            get { return this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 7].As<int>(); }
            set { this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 7].Value = value; }
        }

        public int SRC_POSITION
        {
            get { return this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 8].As<int>(); }
            set { this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 8].Value = value; }
        }

        public int DST_DEVICE
        {
            get { return this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 9].As<int>(); }
            set { this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 9].Value = value; }
        }

        public int DST_SIDE
        {
            get { return this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 10].As<int>(); }
            set { this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 10].Value = value; }
        }

        public int DST_LEVEL
        {
            get { return this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 11].As<int>(); }
            set { this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 11].Value = value; }
        }

        public int DST_POSITION
        {
            get { return this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 12].As<int>(); }
            set { this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 12].Value = value; }
        }

        public int BARCODE
        {
            get { return this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 13].As<int>(); }
            set { this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 13].Value = value; }
        }

    }
}
