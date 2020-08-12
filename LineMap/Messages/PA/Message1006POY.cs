using PLCConnector;
using PLCConnector.L2;
using System;
using System.Collections.Generic;
using System.Text;

namespace LineMap.Messages.PA
{
    public class Message1006POY : Message1006
    {

        public Message1006POY(DataBlock other) : base(other)
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
            this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 11].AsSiemensChars();

        public float BG_EMPTY_TUBE_WEIGHT => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 12].As<int>();

        public int BG_POSITION_ID => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 13].As<int>();

        public DateTime BG_START_TIME_DATE => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 14].As<int>().AsBarmagDate();

        public TimeSpan BG_START_TIME_TIME => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 15].As<int>().AsBarmagTime();

        public DateTime BG_END_TIME_DATE => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 16].As<int>().AsBarmagDate();

        public TimeSpan BG_END_TIME_TIME => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 17].As<int>().AsBarmagTime();

        public DateTime BG_DOFF_TIME_DATE => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 18].As<int>().AsBarmagDate();

        public TimeSpan BG_DOFF_TIME_TIME => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 19].As<int>().AsBarmagTime();

        public int BG_DOFF_NO => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 20].As<int>();

        public TimeSpan BG_PACKAGE_WINDING_TIME => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 21].As<int>().AsBarmagTime();

        public float BG_PACKAGE_DIAMETER => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 22].As<int>();

        public bool[] BG_INFO_ON_END => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 23].AsBitMap();

        public bool[] BG_ADDITIONAL_INFORMATION => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 24].AsBitMap();

        public bool[] BG_WINDING_TIME_INFORMATION => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 25].AsBitMap();

        public bool[] BG_PACKAGE_AVAILABLE => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 26].AsBitMap();

        public float BG_CALCULATED_NET_WEIGHT => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 27].As<int>();

        public bool[] BG_SAMPLE_PACKAGE_DECLARATION => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 28].AsBitMap();

        public bool[] BG_QUALITY_OF_PACKAGE_1_4 => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 29].AsBitMap();

        public bool[] BG_QUALITY_OF_PACKAGE_5_8 => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 30].AsBitMap();

        public bool[] BG_QUALITY_OF_PACKAGE_9_12 => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 31].AsBitMap();

        public bool[] BG_QUALITY_OF_PACKAGE_13_16 => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 32].AsBitMap();

        public bool[] BG_QUALITY_OF_PACKAGE_17_20 => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 33].AsBitMap();

        public bool[] BG_QUALITY_OF_PACKAGE_21_24 => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 34].AsBitMap();

        public bool[] BG_QUALITY_OF_PACKAGE_25_28 => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 35].AsBitMap();

        public bool[] BG_QUALITY_OF_PACKAGE_29_32 => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 36].AsBitMap();

        public int SAL_RESERVE_1 => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 37].As<int>();

        public int SAL_RESERVE_2 => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 38].As<int>();

        public int SAL_RESERVE_3 => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 39].As<int>();

        public int SAL_PLC_LINE => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 40].As<int>();

        public int SAL_PLC_POSITION => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 41].As<int>();

        public int SAL_PLC_DOFF_ID => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 42].As<int>();

        public int SAL_RESERVE_7 => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 43].As<int>();

        public int SAL_RESERVE_8 => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 44].As<int>();

        public int SAL_RESERVE_9 => this[L2HandshakeProtocol.L2_MESASGE_HEADER_SIZE + 45].As<int>();

    }
}
