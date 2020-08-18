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

        readonly ILogger log;

        const int MISSION_TYPE_PICK = 1;
        const int MISSION_TYPE_DEPOSIT = 2;
        const int MISSION_TYPE_MOVE = 3;

        const int STEP_RESULT_OK = 1;
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
            this.log = log;
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
            var l2_handshake_db_raw = new byte[L2HandshakeProtocol.DINT_SIZE];
            Client.WriteData(ref l2_handshake_db_raw, l2_handshake_db);
            Client.WriteRawData(db_number, 0, l2_handshake_db_raw.Length, l2_handshake_db_raw);
        }

        byte[] GetL2BufferToSendMessages(IEnumerable<GenericL2Message> messages)
        {
            var buffer_db = L2HandshakeProtocol.GetL2SendBuffer(messages);
            int total_msg_len = messages.Sum(m => m.MSG_LEN);
            var plc_buffer = new byte[(total_msg_len + 1) * L2HandshakeProtocol.DINT_SIZE];

            Client.WriteData(ref plc_buffer, buffer_db);

            return plc_buffer;
        }

        void DisplayFIFOINStatus()
        {
            log.Information($"FIFO_IN_POS_WRITE {ReadL2HandshakeData(Device.FifoInPosDBNumber)} - FIFO_IN_POS_READ {ReadL2HandshakeData(Device.FifoInDBNumber)}");
        }

        bool PrepareFIFOToSend()
        {
            var l2_fifo_in_pos_read = ReadL2HandshakeData(Device.FifoInPosDBNumber);
            var l2_fifo_in_pos_write = ReadL2HandshakeData(Device.FifoInDBNumber);

            if (l2_fifo_in_pos_read != 0)
            {
                log.Debug($"Emptying FIFO_IN and FIFO_IN_POS with {l2_fifo_in_pos_read} read words");

                WriteL2HandshakeData(Device.FifoInDBNumber, 0);
                WriteL2HandshakeData(Device.FifoInPosDBNumber, 0);

                return false;
            }
            else if (l2_fifo_in_pos_read == 0 && l2_fifo_in_pos_write == 0)
            {
                log.Debug($"FIFO_IN ready to accept commands");
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
                log.Debug($"Emptying FIFO_OUT with {total_words_read} read words and {messages.Count()} total messages");
                WriteL2HandshakeData(Device.FifoOutPosDBNumber, total_words_read);
            }

            return messages;
        }

        void ChainMissions(int from_side, int from_level, int from_pos, int to_side, int to_level, int to_pos, bool auto_next = false, bool shuffle = false)
        {
            log.Debug($"Starting chain from Side {from_side} Level {from_level} Position {from_pos} to Side {to_side} Level {to_level} Position {to_pos}");
            log.Debug($"Shuffle: {shuffle}");
            log.Debug($"Auto Next: {auto_next}");

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

            log.Debug($"Total missions: {missions.Count}");

            if (shuffle)
            {
                var rnd = new Random();
                missions = missions.OrderBy(m => rnd.Next()).ToList();
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
                    log.Error("Invalid pick result received");
                    stop = true;
                }
                else if (pick_result.MISSION_RESULT == MISSION_RESULT_ABORTED && pick_result.STEP_RESULT == STEP_RESULT_RACK_ON_BOARD)
                {
                    log.Error("Can't pick, SA has rack on board");
                    stop = true;
                }
                else if (pick_result.MISSION_RESULT == MISSION_RESULT_ABORTED && pick_result.STEP_RESULT == STEP_RESULT_EMPTY_TO_EMPTY)
                {
                    log.Error("Skipping empty cell");
                }
                else if (pick_result.MISSION_RESULT == MISSION_RESULT_ABORTED)
                {
                    log.Error("Mission aborted, stopping chain.");
                    stop = true;
                }
                else
                {
                    var deposit_result = new Message2013(SendDeposit(DEVICE_WAREHOUSE, side, level, position, false));

                    if (!CheckResultCorrectness(deposit_result))
                    {
                        log.Error("Invalid deposit result received");
                        stop = true;
                    }
                    else if (deposit_result.MISSION_RESULT == MISSION_RESULT_ABORTED)
                    {
                        log.Error("Deposit mission aborted, stopping chain.");
                        stop = true;
                    }
                }

                if (!auto_next && !stop)
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
                            log.Verbose("Activating auto next on chain");
                            auto_next = true;
                            break;
                        }
                        else
                            continue;
                    }
                }

                log.Debug($"Remaining {missions.Count - (m + 1)} missions");
            }

            if (stop)
                log.Warning("Chain stopped");
            else
                log.Information("Chain successfully completed");
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
                    log.Debug("Still waiting for result");

                if (IntervalReached(start_time_fifo_ready, 1))
                    fifo_in_ready = PrepareFIFOToSend();
            }

            var to_write_on_plc = GetL2BufferToSendMessages(new List<Message2012>() { mission });
            Client.WriteRawData(Device.FifoInDBNumber, 0, to_write_on_plc.Length, to_write_on_plc);

            log.Debug("Mission sent, waiting for results...");

            var results = DequeueAllMessages();
            var start_time_queue = DateTime.UtcNow;

            while (results.Count() == 0)
            {
                Thread.Sleep(SLEEP_TIME);

                if (IntervalReached(start_time_queue, 40))
                    log.Debug("Still waiting for result");

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
            return mission_type switch
            {
                MISSION_TYPE_PICK => "PICK",
                MISSION_TYPE_DEPOSIT => "DEPOSIT",
                MISSION_TYPE_MOVE => "MOVE",
                _ => "UNKNOWN",
            };
        }

        string DeviceDescription(int device)
        {
            return device switch
            {
                DEVICE_WAREHOUSE => "WAREHOUSE",
                DEVICE_RACK_HANDLER => "RACK HANDLER",
                DEVICE_RACK_INSERTION_STATION => "RACK INSERTION STATION",
                _ => "UNKNOWN",
            };
        }


        void DisplayMessages(IEnumerable<DataBlock> results)
        {
            foreach (var result in results)
            {
                var result_msg = new Message2013(result);
                log.Information($"MISSION {MissionTypeDescription(result_msg.MISSION_TYPE)} - DEVICE {DeviceDescription(result_msg.DEVICE)} - SIDE {result_msg.SIDE} - LEVEL {result_msg.LEVEL} - POSITION {result_msg.POSITION} - STEP_RESULT {result_msg.STEP_RESULT} - MISSION_RESULT {result_msg.MISSION_RESULT}");
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
                log.Information("No messages");
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
                    log.Information("All cells made empty");
                }
                else if (command == "all full")
                {
                    MakeAllSameID(MOCK_RACK_ID);
                    log.Information("All cells made full");
                }
                else if (command == "display empty cells")
                {
                    for (int side = 0; side < WAREHOUSE_SIDE_MAX; side++)
                    {
                        for (int level = 0; level < WAREHOUSE_LEVEL_MAX; level++)
                        {
                            for (int position = 0; position < WAREHOUSE_POSITION_MAX; position++)
                            {
                                if (Racks[side,level,position] == EMPTY_CELL_RACK_ID)
                                    log.Information($"Side {side + 1} Level {level + 1} Position {position + 1} Empty");
                            }
                        }
                    }
                }
                else if (command.StartsWith("make cell"))
                {
                    var args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (args.Length != 6)
                    {
                        log.Error("Invalid command format");
                        continue;
                    }

                    int side, level, position;

                    try
                    {
                        side = int.Parse(args[2]);
                        level = int.Parse(args[3]);
                        position = int.Parse(args[4]);
                    }
                    catch (FormatException)
                    {
                        log.Error("Error parsing command");
                        continue;
                    }

                    if (!CheckDeviceLimits(DEVICE_WAREHOUSE, side, level, position))
                    {
                        log.Error("Invalid warehouse limits");
                        continue;
                    }

                    var operation = args[5].ToLower().Trim();

                    if (operation == "full")
                    {
                        Racks[side, level, position] = MOCK_RACK_ID;
                        log.Information($"Side {side} Level {level} Position {position} made full");
                    }
                    else if (operation == "empty")
                    {
                        Racks[side, level, position] = EMPTY_CELL_RACK_ID;
                        log.Information($"Side {side} Level {level} Position {position} made empty");
                    }
                    else
                    {
                        log.Error("Invalid operation");
                        continue;
                    }
                }
                else if (command == "empty fifo out")
                {
                    DequeueAllMessages();
                    log.Information("L2 fifo emptied");
                }
                else if (command.StartsWith("pick") || command.StartsWith("dep"))
                {
                    var args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (args.Length != 4)
                    {
                        log.Error("Invalid command format");
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
                        log.Error("Error parsing command");
                        continue;
                    }

                    if (!CheckDeviceLimits(DEVICE_WAREHOUSE, side, level, position))
                    {
                        log.Error("Invalid warehouse limits");
                        continue;
                    }

                    if (command.StartsWith("pick"))
                        SendPick(DEVICE_WAREHOUSE, side, level, position);
                    else if (command.StartsWith("dep"))
                        SendDeposit(DEVICE_WAREHOUSE, side, level, position);
                }
                else if (command.StartsWith("chain from"))
                {
                    // chain from 1 1 1 to 1 2 2 autonext shuffle
                    var args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    int from_side, from_level, from_position, to_side, to_level, to_position;

                    bool auto_next = args.Contains("autonext");
                    bool shuffle = args.Contains("shuffle");

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
                        log.Error("Error parsing command");
                        continue;
                    }

                    if (!CheckDeviceLimits(DEVICE_WAREHOUSE, from_side, from_level, from_position) ||
                        !CheckDeviceLimits(DEVICE_WAREHOUSE, to_side, to_level, to_position) ||
                        to_side < from_side || to_level < from_level || to_position < from_position)
                    {
                        log.Error("Invalid warehouse limits");
                        continue;
                    }

                    ChainMissions(from_side, from_level, from_position, to_side, to_level, to_position, auto_next, shuffle);
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
                    log.Information("Unknown command");
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
                L2HandshakeProtocol.CheckMessageCorrectness(Device.IDPlc, 1, Message2013.MESSAGE_ID, result) &&

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
                (result.MISSION_RESULT >= 1 && result.MISSION_RESULT <= 4) &&

                // Abort management
                ((result.MISSION_RESULT == MISSION_RESULT_COMPLETED && result.STEP_RESULT == STEP_RESULT_OK) || result.MISSION_RESULT != MISSION_RESULT_COMPLETED) &&
                ((result.MISSION_RESULT == MISSION_RESULT_ABORTED && result.STEP_RESULT != STEP_RESULT_OK) || result.MISSION_RESULT != MISSION_RESULT_ABORTED);
        }

        Message2012 GetEmptyMessage2012()
        {
            return new Message2012(L2HandshakeProtocol.GetL2MessageFromNumberOfDataFields(Message2012.MESSAGE_DATA_FIELDS_NUM))
            {
                PR_MSG = L2HandshakeProtocol.GetNextProgressive(),
                ID_PLC = Device.IDPlc
            };
        }

    }
}
