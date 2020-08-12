using PLCConnector;
using PLCConnector.L2;
using System;
using System.Collections.Generic;
using System.Text;

namespace LineMap.Messages.PA
{
    public class Message1006FDY : Message1006
    {

        public Message1006FDY(DataBlock other) : base(other)
        {

        }

        public String BG_MACHINE_NR => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 3].AsSiemensChars();

        public String BG_POSITION_NR => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 4].AsSiemensChars();

        public String BG_PRODUCT_DATA => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 5].AsSiemensChars() +
            this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 6].AsSiemensChars() +
            this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 7].AsSiemensChars() +
            this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 8].AsSiemensChars() +
            this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 9].AsSiemensChars() +
            this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 10].AsSiemensChars() +
            this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 11].AsSiemensChars() +
            this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 12].AsSiemensChars();

    }
}
