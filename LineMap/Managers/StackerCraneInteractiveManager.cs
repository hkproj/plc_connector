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
    public class StackerCraneInteractiveManager : StackerCraneManager
    {

        readonly ILogger Log;

        int[,,] Racks { get; set; }

        const int COLUMNS = 31;
        const int ROWS = 8;
        const int SIDES = 2;

        const int EMPTY_CELL_RACK_ID = 0;
        const int MOCK_RACK_ID = 1992;
        const int MOCK_ORDER_ID = 147;
        const int MOCK_MISSION_ID = 852;

        const int SLEEP_TIME = 50;

        public StackerCraneInteractiveManager(DeviceConfiguration device, ILogger log) : base(device)
        {
            this.Log = log;
            this.Racks = new int[SIDES, ROWS, COLUMNS];
        }

        void MakeAllSameID(int rack_id)
        {
            for (int s = 0; s < SIDES; s++)
            {
                for (int r = 0; r < ROWS; r++)
                {
                    for (int c = 0; c < COLUMNS; c++)
                    {
                        Racks[s, r, c] = rack_id;
                    }
                }
            }
        }

        void DisplayFIFOINStatus()
        {
            var l2_fifo_in_pos_read_raw = Client.ReadRawData(Device.FifoInPosDBNumber, 0, L2HandshakeProtocol.DINT_SIZE);
            var l2_fifo_in_pos_read_db = L2HandshakeProtocol.GetL2HandshakeResponseDescriptor();
            l2_fifo_in_pos_read_db = Client.ReadData(l2_fifo_in_pos_read_raw, ref l2_fifo_in_pos_read_db);

            var l2_fifo_in_pos_write_raw = Client.ReadRawData(Device.FifoInDBNumber, 0, L2HandshakeProtocol.DINT_SIZE);
            var l2_fifo_in_pos_write_db = L2HandshakeProtocol.GetL2HandshakeResponseDescriptor();
            l2_fifo_in_pos_write_db = Client.ReadData(l2_fifo_in_pos_write_raw, ref l2_fifo_in_pos_write_db);

            Log.Information($"FIFO_IN_POS_WRITE {l2_fifo_in_pos_write_db.Fields[0].As<int>()} - FIFO_IN_POS_READ {l2_fifo_in_pos_read_db.Fields[0].As<int>()}");
        }

        bool PrepareFIFOToSend()
        {
            var l2_fifo_in_pos_read_raw = Client.ReadRawData(Device.FifoInPosDBNumber, 0, L2HandshakeProtocol.DINT_SIZE);
            var l2_fifo_in_pos_read_db = L2HandshakeProtocol.GetL2HandshakeResponseDescriptor();
            l2_fifo_in_pos_read_db = Client.ReadData(l2_fifo_in_pos_read_raw, ref l2_fifo_in_pos_read_db);

            var l2_fifo_in_pos_write_raw = Client.ReadRawData(Device.FifoInDBNumber, 0, L2HandshakeProtocol.DINT_SIZE);
            var l2_fifo_in_pos_write_db = L2HandshakeProtocol.GetL2HandshakeResponseDescriptor();
            l2_fifo_in_pos_write_db = Client.ReadData(l2_fifo_in_pos_write_raw, ref l2_fifo_in_pos_write_db);

            if (l2_fifo_in_pos_read_db.Fields[0].As<int>() != 0)
            {
                Log.Debug($"Emptying FIFO_IN and FIFO_IN_POS with {l2_fifo_in_pos_read_db.Fields[0].As<int>()} read words");

                l2_fifo_in_pos_write_db = L2HandshakeProtocol.GetL2HandshakeResponseDescriptor();
                l2_fifo_in_pos_write_db.Fields[0].Value = 0;
                var handshake_response_raw = new byte[4];
                Client.WriteData(ref handshake_response_raw, l2_fifo_in_pos_write_db);
                Client.WriteRawData(Device.FifoInDBNumber, 0, handshake_response_raw.Length, handshake_response_raw);

                l2_fifo_in_pos_read_db = L2HandshakeProtocol.GetL2HandshakeResponseDescriptor();
                l2_fifo_in_pos_read_db.Fields[0].Value = 0;
                handshake_response_raw = new byte[4];
                Client.WriteData(ref handshake_response_raw, l2_fifo_in_pos_read_db);
                Client.WriteRawData(Device.FifoInPosDBNumber, 0, handshake_response_raw.Length, handshake_response_raw);

                return false;
            }
            else if (l2_fifo_in_pos_read_db.Fields[0].As<int>() == 0 && l2_fifo_in_pos_write_db.Fields[0].As<int>() == 0)
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
                var handshake_response = L2HandshakeProtocol.GetL2HandshakeResponse(total_words_read);
                var handshake_response_raw = new byte[4];
                Client.WriteData(ref handshake_response_raw, handshake_response);
                Client.WriteRawData(Device.FifoOutPosDBNumber, 0, handshake_response_raw.Length, handshake_response_raw);
            }

            return messages;
        }

        void SendDeposit(int side, int row, int pos, bool dequeue_first = true)
        {
            var mission = DepositMission(MOCK_ORDER_ID, MOCK_MISSION_ID, Device.IDDevice, 1, side, row, pos, MOCK_RACK_ID);
            SendMissionAndDisplayResult(mission, dequeue_first);
        }

        void ChainMissions(int from_side, int from_level, int from_pos, int to_side, int to_level, int to_pos)
        {
            Log.Debug($"Starting chain from Side {from_side} Level {from_level} Position {from_pos} to Side {to_side} Level {to_level} Position {to_pos}");

            var stop = false;
            var next = false;
            var auto_next = false;

            for (int side = from_side; side <= to_side; side++)
            {
                if (stop)
                    break;

                for (var level = (side == from_side ? from_level : 1); level <= to_level; level++)
                {
                    if (stop)
                        break;

                    for (int position = (level == from_level && side == from_side ? from_pos : 1); position <= to_pos; position++)
                    {
                        if (stop)
                            break;

                        next = false;

                        SendPick(side, level, position, (side == from_side && level == from_level && position == from_pos));
                        SendDeposit(side, level, position, false);

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
                                    auto_next = true;
                                    break;
                                }
                                else
                                    continue;
                            }
                        }
                    }
                }
            }

            if (stop)
                Log.Warning("Chain stopped");
            else
                Log.Information("Chain successfully completed");
        }

        void SendMissionAndDisplayResult(Message2012 mission, bool dequeue_first = true)
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

            var mission_raw = new byte[mission.MSG_LEN * L2HandshakeProtocol.DINT_SIZE];
            Client.WriteData(ref mission_raw, mission);

            var l2_fifo_in_pos_write_db = L2HandshakeProtocol.GetL2HandshakeResponseDescriptor();
            l2_fifo_in_pos_write_db.Fields[0].Value = mission.MSG_LEN;
            var l2_fifo_in_pos_write_raw = new byte[4];
            Client.WriteData(ref l2_fifo_in_pos_write_raw, l2_fifo_in_pos_write_db);

            var to_write_on_plc = new byte[mission_raw.Length + l2_fifo_in_pos_write_raw.Length];
            Array.Copy(l2_fifo_in_pos_write_raw, 0, to_write_on_plc, 0, l2_fifo_in_pos_write_raw.Length);
            Array.Copy(mission_raw, 0, to_write_on_plc, l2_fifo_in_pos_write_raw.Length, mission_raw.Length);

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
        }

        bool IntervalReached(DateTime start_time, int interval)
        {
            var interval_so_far = (int)(DateTime.UtcNow - start_time).TotalSeconds;
            return (interval_so_far > interval && (interval_so_far % interval == 0));
        }

        void SendPick(int side, int row, int pos, bool dequeue_first = true)
        {
            var mission = PickMission(MOCK_ORDER_ID, MOCK_MISSION_ID, Device.IDDevice, 1, side, row, pos, MOCK_RACK_ID);
            SendMissionAndDisplayResult(mission, dequeue_first);
        }

        string MissionTypeDescription(int mission_type)
        {
            switch (mission_type)
            {
                case 1:
                    return "PICK";
                case 2:
                    return "DEPOSIT";
                case 3:
                    return "MOVE";
                default:
                    return "UNKNOWN";
            }
        }

        void DisplayMessages(IEnumerable<DataBlock> results)
        {
            foreach (var result in results)
            {
                var result_msg = new Message2013(result);
                Log.Information($"MISSION {MissionTypeDescription(result_msg.MISSION_TYPE)} - DEVICE {result_msg.DEVICE} - SIDE {result_msg.SIDE} - LEV {result_msg.LEVEL} - POS {result_msg.POSITION} - STEP_RESULT {result_msg.STEP_RESULT} - MISSION_RESULT {result_msg.MISSION_RESULT}");
            }
        }

        void DisplayResult()
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

                    if (command.StartsWith("pick"))
                        SendPick(side, level, position);
                    else if (command.StartsWith("dep"))
                        SendDeposit(side, level, position);
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

                    if ((from_side < 1 || from_side > 2 || to_side < 1 || to_side > 2 || to_side < from_side) ||
                        (from_level < 1 || from_level > 8 || to_level < 1 || to_level > 8 || to_level < from_level) ||
                        (from_position < 1 || from_position > 31 || to_position < 1 || to_position > 31 || to_position < from_position))
                    {
                        Log.Error("Invalid command format");
                        continue;
                    }

                    ChainMissions(from_side, from_level, from_position, to_side, to_level, to_position);
                }
                else if (command == "display result")
                {
                    DisplayResult();
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

    }
}
