using LineMap.Messages.SA;
using PLCConnector.L2;
using System;
using System.Collections.Generic;
using System.Text;

namespace LineMap.Managers
{
    public class StackerCraneManager
    {

        const int MISSION_TYPE_PICK = 1;
        const int MISSION_TYPE_DEPOSIT = 2;
        const int MISSION_TYPE_MOVE = 3;

        public int ID_PLC { get; set; }

        public StackerCraneManager(int id_plc)
        {
            this.ID_PLC = id_plc;
        }

        public Message2012 PickMission(int order_id, int mission_id, int device_nr, int src_device, int src_side, int src_level, int src_position, int track_id)
        {
            var message = GetEmptyMessage2012();

            message.ORDER_ID = order_id;
            message.MISSION_ID = mission_id;
            message.DEVICE_NR = device_nr;
            message.MISSION_TYPE = MISSION_TYPE_PICK;
            message.TRACK_ID = track_id;

            message.SRC_DEVICE = src_device;
            message.SRC_SIDE = src_side;
            message.SRC_LEVEL = src_level;
            message.SRC_POSITION = src_position;

            return message;
        }

        public Message2012 DepositMission(int order_id, int mission_id, int device_nr, int dst_device, int dst_side, int dst_level, int dst_position, int track_id)
        {
            var message = GetEmptyMessage2012();

            message.ORDER_ID = order_id;
            message.MISSION_ID = mission_id;
            message.DEVICE_NR = device_nr;
            message.MISSION_TYPE = MISSION_TYPE_DEPOSIT;
            message.TRACK_ID = track_id;

            message.DST_DEVICE = dst_device;
            message.DST_SIDE = dst_side;
            message.DST_LEVEL = dst_level;
            message.DST_POSITION = dst_position;

            return message;
        }

        public Message2012 MoveMission(int order_id, int mission_id, int device_nr, int dst_device, int dst_side, int dst_level, int dst_position, int track_id)
        {
            var message = GetEmptyMessage2012();

            message.ORDER_ID = order_id;
            message.MISSION_ID = mission_id;
            message.DEVICE_NR = device_nr;
            message.MISSION_TYPE = MISSION_TYPE_MOVE;
            message.TRACK_ID = track_id;

            message.DST_DEVICE = dst_device;
            message.DST_SIDE = dst_side;
            message.DST_LEVEL = dst_level;
            message.DST_POSITION = dst_position;

            return message;
        }

        public bool CheckResultCorrectness(Message2013 result)
        {
            return 
                L2HandshakeProtocol.CheckMessageCorrectness(ID_PLC, 1, 2012, result) &&

                (result.ORDER_ID != 0) &&
                (result.MISSION_ID != 0) &&
                (result.DEVICE_NR >= 3401 && result.DEVICE_NR <= 3405) &&
                (result.MISSION_TYPE >= 1 && result.MISSION_TYPE <= 3) &&
                (result.TRACK_ID != 0) &&
                (result.DEVICE >= 1 && result.DEVICE <= 3) &&

                // Side, level and position for each device
                ((result.DEVICE == 1 && result.SIDE >= 1 && result.SIDE <= 2) || (result.DEVICE == 2 && result.SIDE == 1) || (result.DEVICE == 3 && result.SIDE == 2)) &&
                ((result.DEVICE == 1 && result.LEVEL >= 1 && result.LEVEL <= 8) || (result.DEVICE == 2 && result.LEVEL == 0) || (result.DEVICE == 3 && result.LEVEL == 0)) &&
                ((result.DEVICE == 1 && result.POSITION >= 1 && result.POSITION <= 31) || (result.DEVICE == 2 && result.POSITION == 0) || (result.DEVICE == 3 && result.POSITION == 0)) &&

                // Results
                (result.STEP_RESULT >= 1 && result.STEP_RESULT <= 11) &&
                (result.MISSION_RESULT >= 1 && result.MISSION_RESULT <= 4);
        }

        Message2012 GetEmptyMessage2012()
        {
            var message = new Message2012(L2HandshakeProtocol.GetL2MessageFromDataFields(new List<string>()
            {
                nameof(Message2012.ORDER_ID),
                nameof(Message2012.MISSION_ID),
                nameof(Message2012.DEVICE_NR),
                nameof(Message2012.MISSION_TYPE),
                nameof(Message2012.TRACK_ID),
                nameof(Message2012.SRC_DEVICE),
                nameof(Message2012.SRC_SIDE),
                nameof(Message2012.SRC_LEVEL),
                nameof(Message2012.SRC_POSITION),
                nameof(Message2012.DST_DEVICE),
                nameof(Message2012.DST_SIDE),
                nameof(Message2012.DST_LEVEL),
                nameof(Message2012.DST_POSITION),
                nameof(Message2012.BARCODE)
            }));

            message.MSG_LEN = 20;
            message.PR_MSG = L2HandshakeProtocol.GetNextProgressive();
            message.ID_MSG = 2012;
            message.ID_SRC = 1;
            message.ID_PLC = ID_PLC;
            message.FOOTER = 20;

            return message;
        }

    }
}
