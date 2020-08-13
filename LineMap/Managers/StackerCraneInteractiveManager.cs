using PLCConnector.L2;
using PLCConnector.Siemens;
using Serilog;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using PLCConnector;
using LineMap.Messages.SA;
using System.Threading;

namespace LineMap.Managers
{
    public class StackerCraneInteractiveManager
    {

        readonly ILogger Log;

        const int MISSION_TYPE_PICK = 1;
        const int MISSION_TYPE_DEPOSIT = 2;
        const int MISSION_TYPE_MOVE = 3;

        const int STEP_RESULT_RACK_ON_BOARD = 2;
        const int STEP_RESULT_EMPTY_TO_EMPTY = 5;

        const int MISSION_RESULT_COMPLETED = 1;
        const int MISSION_RESULT_ABORTED = 2;

        protected DeviceConfiguration Device { get; }

        protected SiemensClient Client { get; }

        int[,,] Racks { get; set; }

        const int EMPTY_CELL_RACK_ID = 0;
        const int MOCK_RACK_ID = 1992;
        const int MOCK_ORDER_ID = 147;
        const int MOCK_MISSION_ID = 852;

        const int WAREHOUSE_SIDE_MIN = 1;
        const int WAREHOUSE_SIDE_MAX = 2;
        const int WAREHOUSE_LEVEL_MIN = 1;
        const int WAREHOUSE_LEVEL_MAX = 8;
        const int WAREHOUSE_POSITION_MIN = 1;
        const int WAREHOUSE_POSITION_MAX = 31;

        const int DEVICE_WAREHOUSE = 1;
        const int DEVICE_RACK_HANDLER = 2;
        const int DEVICE_RACK_INSERTION_STATION = 3;

        const int SLEEP_TIME = 50;

        public StackerCraneInteractiveManager(DeviceConfiguration device, ILogger log)
        {
            this.Device = device;
            this.Client = new SiemensClient(Device.IPAddress);
            this.Log = log;
            this.Racks = new int[WAREHOUSE_SIDE_MAX, WAREHOUSE_LEVEL_MAX, WAREHOUSE_POSITION_MAX];
        }

        void MakeAllSameID(int rack_id)
        {
            for (int s = 0; s < WAREHOUSE_SIDE_MAX; s++)
            {
                for (int l = 0; l < WAREHOUSE_LEVEL_MAX; l++)
                {
                    for (int c = 0; c < WAREHOUSE_POSITION_MAX; c++)
                    {
                        Racks[s, l, c] = rack_id;
                    }
                }
            }
        }

        int ReadL2HandshakeData(int db_number)
        {
            var l2_handshake_db_raw = Client.ReadRawData(db_number, 0, L2HandshakeProtocol.DINT_SIZE);
            var l2_handshake_db = L2HandshakeProtocol.GetL2HandshakeDescriptor();
            l2_handshake_db = Client.ReadData(l2_handshake_db_raw, ref l2_handshake_db);

            return l2_handshake_db.Fields[0].As<int>();
        }

        void WriteL2HandshakeData(int db_number, int value)
        {
            var l2_handshake_db = L2HandshakeProtocol.GetL2HandshakeDescriptor();
            l2_handshake_db.Fields[0].Value = value;
            var l2_handshake_db_raw = new byte[4];
            Client.WriteData(ref l2_handshake_db_raw, l2_handshake_db);
            Client.WriteRawData(db_number, 0, l2_handshake_db_raw.Length, l2_handshake_db_raw);
        }

        byte[] GetL2BufferToSendMessages(IEnumerable<GenericL2Message> messages)
        {
            int total_msg_len = messages.Sum(m => m.MSG_LEN);
            var plc_buffer = new byte[(total_msg_len + 1) * L2HandshakeProtocol.DINT_SIZE];
            int current_offset = L2HandshakeProtocol.DINT_SIZE;

            for (var i = 0; i < messages.Count(); i++)
            {
                var msg = messages.ElementAt(i);
                var msg_buffer = new byte[msg.MSG_LEN * L2HandshakeProtocol.DINT_SIZE];
                Client.WriteData(ref msg_buffer, msg);

                Array.Copy(msg_buffer, 0, plc_buffer, current_offset, msg_buffer.Length);
                current_offset += msg_buffer.Length;
            }

            var l2_fifo_in_pos_write_db = L2HandshakeProtocol.GetL2HandshakeDescriptor();
            l2_fifo_in_pos_write_db.Fields[0].Value = total_msg_len;
            var l2_fifo_in_pos_write_raw = new byte[L2HandshakeProtocol.DINT_SIZE];
            Client.WriteData(ref l2_fifo_in_pos_write_raw, l2_fifo_in_pos_write_db);

            Array.Copy(l2_fifo_in_pos_write_raw, 0, plc_buffer, 0, l2_fifo_in_pos_write_raw.Length);

            return plc_buffer;
        }

        void DisplayFIFOINStatus()
        {
            Log.Information($"FIFO_IN_POS_WRITE {ReadL2HandshakeData(Device.FifoInPosDBNumber)} - FIFO_IN_POS_READ {ReadL2HandshakeData(Device.FifoInDBNumber)}");
        }

        bool PrepareFIFOToSend()
        {
            var l2_fifo_in_pos_read = ReadL2HandshakeData(Device.FifoInPosDBNumber);
            var l2_fifo_in_pos_write = ReadL2HandshakeData(Device.FifoInDBNumber);

            if (l2_fifo_in_pos_read != 0)
            {
                Log.Debug($"Emptying FIFO_IN and FIFO_IN_POS with {l2_fifo_in_pos_read} read words");

                WriteL2HandshakeData(Device.FifoInDBNumber, 0);
                WriteL2HandshakeData(Device.FifoInPosDBNumber, 0);

                return false;
            }
            else if (l2_fifo_in_pos_read == 0 && l2_fifo_in_pos_write == 0)
            {
                Log.Debug($"FIFO_IN ready to accept commands");
                return true;
            }
            else
            {
                throw new InvalidProgramException("FIFO_IN in an inconsistent state");
            }
        }

        IEnumerable<DataBlock> DequeueAllMessages()
        {
            var l2_db_raw = Client.ReadRawData(Device.FifoOutDBNumber, 0, L2HandshakeProtocol.L2_DB_SIZE_DINT * L2HandshakeProtocol.DINT_SIZE);
            var l2_db = L2HandshakeProtocol.GetL2DBDescriptor();
            l2_db = Client.ReadData(l2_db_raw, ref l2_db);

            var messages = L2HandshakeProtocol.GetMessagesFromL2DB(l2_db, out int total_words_read);

            if (messages.Count() > 0)
            {
                Log.Debug($"Emptying FIFO_OUT with {total_words_read} read words and {messages.Count()} total messages");
                WriteL2HandshakeData(Device.FifoOutPosDBNumber, total_words_read);
            }

            return messages;
        }

        void ChainMissions(int from_side, int from_level, int from_pos, int to_side, int to_level, int to_pos, bool auto_next = false)
        {
            Log.Debug($"Starting chain from Side {from_side} Level {from_level} Position {from_pos} to Side {to_side} Level {to_level} Position {to_pos}");

            var stop = false;

            var missions = new List<Tuple<int, int, int>>();

            for (int side = from_side; side <= to_side; side++)
            {
                for (var level = (side == from_side ? from_level : 1); level <= to_level; level++)
                {
                    for (int position = (level == from_level && side == from_side ? from_pos : 1); position <= to_pos; position++)
                    {
                        missions.Add(new Tuple<int, int, int>(side, level, position));
                    }
                }
            }

            for (int m = 0; m < missions.Count; m++)
            {
                if (stop)
                    break;

                var next = false;

                var (side, level, position) = missions[m];

                var pick_result = new Message2013(SendPick(DEVICE_WAREHOUSE, side, level, position, (side == from_side && level == from_level && position == from_pos)));

                if (!CheckResultCorrectness(pick_result))
                {
                    Log.Error("Invalid pick result received");
                    stop = true;
                }
                else if (pick_result.MISSION_RESULT == MISSION_RESULT_ABORTED && pick_result.STEP_RESULT == STEP_RESULT_RACK_ON_BOARD)
                {
                    Log.Error("Can't pick, SA has rack on board");
                    stop = true;
                }
                else if (pick_result.MISSION_RESULT == MISSION_RESULT_ABORTED && pick_result.STEP_RESULT == STEP_RESULT_EMPTY_TO_EMPTY)
                {
                    Log.Error("Skipping empty cell");
                }
                else
                {
                    var deposit_result = new Message2013(SendDeposit(DEVICE_WAREHOUSE, side, level, position, false));

                    if (!CheckResultCorrectness(deposit_result))
                    {
                        Log.Error("Invalid deposit result received");
                        stop = true;
                    }
                }

                if (!auto_next)
                {
                    Console.WriteLine("Chain: next, autonext or stop");

                    while (!next && !stop)
                    {
                        var command = Console.ReadLine().ToLower().Trim();

                        if (command == "next")
                            next = true;
                        else if (command == "stop")
                            stop = true;
                        else if (command == "autonext")
                        {
                            Log.Verbose("Activating auto next on chain");
                            auto_next = true;
                            break;
                        }
                        else
                            continue;
                    }
                }
            }

            if (stop)
                Log.Warning("Chain stopped");
            else
                Log.Information("Chain successfully completed");
        }

        DataBlock SendMissionAndDisplayResult(Message2012 mission, bool dequeue_first = true)
        {
            if (dequeue_first)
                DequeueAllMessages();

            bool fifo_in_ready = PrepareFIFOToSend();
            var start_time_fifo_ready = DateTime.UtcNow;

            while (!fifo_in_ready)
            {
                Thread.Sleep(SLEEP_TIME);

                if (IntervalReached(start_time_fifo_ready, 5))
                    Log.Debug("Still waiting for result");

                if (IntervalReached(start_time_fifo_ready, 1))
                    fifo_in_ready = PrepareFIFOToSend();
            }

            var to_write_on_plc = GetL2BufferToSendMessages(new List<Message2012>() { mission });
            Client.WriteRawData(Device.FifoInDBNumber, 0, to_write_on_plc.Length, to_write_on_plc);

            Log.Debug("Mission sent, waiting for results...");

            var results = DequeueAllMessages();
            var start_time_queue = DateTime.UtcNow;

            while (results.Count() == 0)
            {
                Thread.Sleep(SLEEP_TIME);

                if (IntervalReached(start_time_queue, 40))
                    Log.Debug("Still waiting for result");

                if (IntervalReached(start_time_queue, 1))
                    results = DequeueAllMessages();
            }

            DisplayMessages(results);
            return results.First();
        }

        bool IntervalReached(DateTime start_time, int interval)
        {
            var interval_so_far = (int)(DateTime.UtcNow - start_time).TotalSeconds;
            return (interval_so_far > interval && (interval_so_far % interval == 0));
        }

        DataBlock SendDeposit(int device, int side, int row, int pos, bool dequeue_first = true)
        {
            var mission = DepositMission(MOCK_ORDER_ID, MOCK_MISSION_ID, Device.IDDevice, device, side, row, pos, MOCK_RACK_ID);
            return SendMissionAndDisplayResult(mission, dequeue_first);
        }

        DataBlock SendPick(int device, int side, int row, int pos, bool dequeue_first = true)
        {
            var mission = PickMission(MOCK_ORDER_ID, MOCK_MISSION_ID, Device.IDDevice, device, side, row, pos, MOCK_RACK_ID);
            return SendMissionAndDisplayResult(mission, dequeue_first);
        }

        string MissionTypeDescription(int mission_type)
        {
            switch (mission_type)
            {
                case MISSION_TYPE_PICK:
                    return "PICK";
                case MISSION_TYPE_DEPOSIT:
                    return "DEPOSIT";
                case MISSION_TYPE_MOVE:
                    return "MOVE";
                default:
                    return "UNKNOWN";
            }
        }

        string DeviceDescription(int device)
        {
            switch (device)
            {
                case DEVICE_WAREHOUSE:
                    return "WAREHOUSE";
                case DEVICE_RACK_HANDLER:
                    return "RACK HANDLER";
                case DEVICE_RACK_INSERTION_STATION:
                    return "RACK INSERTION STATION";
                default:
                    return "UNKNOWN";
            }
        }


        void DisplayMessages(IEnumerable<DataBlock> results)
        {
            foreach (var result in results)
            {
                var result_msg = new Message2013(result);
                Log.Information($"MISSION {MissionTypeDescription(result_msg.MISSION_TYPE)} - DEVICE {DeviceDescription(result_msg.DEVICE)} - SIDE {result_msg.SIDE} - LEVEL {result_msg.LEVEL} - POSITION {result_msg.POSITION} - STEP_RESULT {result_msg.STEP_RESULT} - MISSION_RESULT {result_msg.MISSION_RESULT}");
            }
        }

        void DequeueAndDisplayAllMessages()
        {
            var messages = DequeueAllMessages();

            if (messages.Count() > 0)
            {
                DisplayMessages(messages);
            }
            else
            {
                Log.Information("No messages");
            }
        }


        public void Start()
        {
            Console.WriteLine("Stacker Crane Interactive Manager: starting...");
            var client = new SiemensClient(Device.IPAddress);
            Console.WriteLine("Stacker Crane Interactive Manager: write your command");

            while (true)
            {
                var command = Console.ReadLine().ToLower().Trim();

                if (command == "all empty")
                {
                    MakeAllSameID(EMPTY_CELL_RACK_ID);
                    Log.Information("All cells made empty");
                }
                else if (command == "all full")
                {
                    MakeAllSameID(MOCK_RACK_ID);
                    Log.Information("All cells made full");
                }
                else if (command == "empty fifo out")
                {
                    DequeueAllMessages();
                    Log.Information("L2 fifo emptied");
                }
                else if (command.StartsWith("pick") || command.StartsWith("dep"))
                {
                    var args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (args.Length != 4)
                    {
                        Log.Error("Invalid command format");
                        continue;
                    }

                    int side, level, position;

                    try
                    {
                        side = int.Parse(args[1]);
                        level = int.Parse(args[2]);
                        position = int.Parse(args[3]);
                    }
                    catch (FormatException)
                    {
                        Log.Error("Error parsing command");
                        continue;
                    }

                    if (!CheckDeviceLimits(DEVICE_WAREHOUSE, side, level, position))
                    {
                        Log.Error("Invalid warehouse limits");
                        continue;
                    }

                    if (command.StartsWith("pick"))
                        SendPick(DEVICE_WAREHOUSE, side, level, position);
                    else if (command.StartsWith("dep"))
                        SendDeposit(DEVICE_WAREHOUSE, side, level, position);
                }
                else if (command.StartsWith("chain from"))
                {
                    // chain from 1 1 1 to 1 2 2
                    var args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (args.Length != 9)
                    {
                        Log.Error("Invalid command format");
                        continue;
                    }

                    int from_side, from_level, from_position, to_side, to_level, to_position;

                    try
                    {
                        from_side = int.Parse(args[2]);
                        from_level = int.Parse(args[3]);
                        from_position = int.Parse(args[4]);

                        to_side = int.Parse(args[6]);
                        to_level = int.Parse(args[7]);
                        to_position = int.Parse(args[8]);
                    }
                    catch (FormatException)
                    {
                        Log.Error("Error parsing command");
                        continue;
                    }

                    if (!CheckDeviceLimits(DEVICE_WAREHOUSE, from_side, from_level, from_position) ||
                        !CheckDeviceLimits(DEVICE_WAREHOUSE, to_side, to_level, to_position) ||
                        to_side < from_side || to_level < from_level || to_position < from_position)
                    {
                        Log.Error("Invalid warehouse limits");
                        continue;
                    }

                    ChainMissions(from_side, from_level, from_position, to_side, to_level, to_position);
                }
                else if (command == "display messages")
                {
                    DequeueAndDisplayAllMessages();
                }
                else if (command == "prepare fifo in")
                {
                    PrepareFIFOToSend();
                }
                else if (command == "fifo in status")
                {
                    DisplayFIFOINStatus();
                }
                else if (command == "exit" || command == "quit")
                {
                    break;
                }
                else
                    Log.Information("Unknown command");
            }

            Console.WriteLine("Bye bye");
        }

        Message2012 PickMission(int order_id, int mission_id, int device_nr, int src_device, int src_side, int src_level, int src_position, int track_id)
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

        Message2012 DepositMission(int order_id, int mission_id, int device_nr, int dst_device, int dst_side, int dst_level, int dst_position, int track_id)
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

        Message2012 MoveMission(int order_id, int mission_id, int device_nr, int dst_device, int dst_side, int dst_level, int dst_position, int track_id)
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

        bool CheckDeviceLimits(int device, int side, int level, int position)
        {
            return
                (device == DEVICE_WAREHOUSE && side >= WAREHOUSE_SIDE_MIN && side <= WAREHOUSE_SIDE_MAX && level >= WAREHOUSE_LEVEL_MIN && level <= WAREHOUSE_LEVEL_MAX && position >= WAREHOUSE_POSITION_MIN && position <= WAREHOUSE_POSITION_MAX) ||
                (device == DEVICE_RACK_HANDLER && side == 1 && level == 0 && position == 0) ||
                (device == DEVICE_RACK_INSERTION_STATION && side == 2 && level == 0 && position == 0);
        }

        bool CheckResultCorrectness(Message2013 result)
        {
            return
                L2HandshakeProtocol.CheckMessageCorrectness(Device.IDPlc, 1, 2012, result) &&

                (result.ORDER_ID != 0) &&
                (result.MISSION_ID != 0) &&
                (result.DEVICE_NR >= 3401 && result.DEVICE_NR <= 3405) &&
                (result.MISSION_TYPE >= 1 && result.MISSION_TYPE <= 3) &&
                (result.TRACK_ID != 0) &&
                (result.DEVICE >= 1 && result.DEVICE <= 3) &&

                // Side, level and position for each device
                CheckDeviceLimits(result.DEVICE, result.SIDE, result.LEVEL, result.POSITION) &&

                // Results
                (result.STEP_RESULT >= 1 && result.STEP_RESULT <= 11) &&
                (result.MISSION_RESULT >= 1 && result.MISSION_RESULT <= 4);
        }

        Message2012 GetEmptyMessage2012()
        {
            return new Message2012(L2HandshakeProtocol.GetL2MessageFromDataFields(new List<string>()
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
            }))
            {
                PR_MSG = L2HandshakeProtocol.GetNextProgressive(),
                ID_PLC = Device.IDPlc
            };
        }

    }
}
