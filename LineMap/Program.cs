using PLCConnector;
using System;
using System.Linq;
using System.Collections.Generic;
using Serilog;
using PLCConnector.L2;
using LineMap.Messages;
using PLCConnector.Siemens;
using System.Threading;
using LineMap.Messages.PA;
using LineMap.Managers;

namespace LineMap
{
    class Program
    {

        static ILogger log;

        static void Main(string[] args)
        {
            log = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.ColoredConsole()
                .CreateLogger();

            var sa_manager = new StackerCraneInteractiveManager(new DeviceConfiguration()
            {
                FifoInDBNumber = 300,
                FifoInPosDBNumber = 301,
                FifoOutDBNumber = 302,
                FifoOutPosDBNumber = 303,
                IDDevice = 3400 + 5,
                IDPlc = 40 + 5,
                IPAddress = "172.21.3.150"
            }, log);

            sa_manager.Start();
        }

        //static void PollPAMessages()
        //{
        //    const string PLC_ADDRESS = "172.21.1.100";

        //    while (true)
        //    {
        //        log.Debug($"Polling started");
        //        var client = new SiemensClient(PLC_ADDRESS);
        //        var l2_db_raw = client.ReadRawData(302, 0, L2HandshakeProtocol.L2_DB_SIZE_DINT * L2HandshakeProtocol.DINT_SIZE);

        //        ReadL2Messages(log, client, l2_db_raw, out int total_words_read);

        //        if (total_words_read > 0)
        //        {
        //            log.Debug($"Closing handshake with {total_words_read} read words");
        //            var handshake_response = L2HandshakeProtocol.GetL2HandshakeResponse(total_words_read);
        //            var handshake_response_raw = new byte[4];
        //            client.WriteData(ref handshake_response_raw, handshake_response);
        //            client.WriteRawData(303, 0, handshake_response_raw.Length, handshake_response_raw);
        //        }

        //        Thread.Sleep(1000);
        //    }
        //}

        //static void ReadL2Messages(ILogger log, SiemensClient client, byte[] l2_db_raw, out int total_words_read)
        //{
        //    var l2_db = L2HandshakeProtocol.GetL2DBDescriptor();
        //    l2_db = client.ReadData(l2_db_raw, ref l2_db);
        //    var messages = L2HandshakeProtocol.GetMessagesFromL2DB(l2_db, out total_words_read);

        //    if (messages.Count() > 0)
        //        log.Debug($"Total messages: {messages.Count()}");

        //    foreach (var message in messages)
        //    {
        //        var l2_message = new GenericL2Message(message);

        //        if (l2_message.ID_MSG == 1006)
        //        {
        //            var message_1006 = new Message1006(l2_message);
        //            var is_fdy = message_1006.LINE_NR >= 3 && message_1006.LINE_NR <= 6;

        //            if (is_fdy)
        //            {
        //                var bg_message_1006 = new Message1006FDY(message_1006);
        //                log.Information($"FDY DOFFING - LINE {bg_message_1006.LINE_NR} - POSITION {bg_message_1006.POSITION_NR} - DOFF {bg_message_1006.DOFF_NR} - MACH. {bg_message_1006.BG_MACHINE_NR} - POS. {bg_message_1006.BG_POSITION_NR}");
        //            }
        //            else
        //            {
        //                var bg_message_1006 = new Message1006POY(message_1006);
                        
        //                log.Information($"POY DOFFING - LINE({bg_message_1006.LINE_NR}),POSITION({bg_message_1006.POSITION_NR}),DOFF({bg_message_1006.DOFF_NR}),MACHINE({bg_message_1006.BG_MACHINE_NR}),POSITION({bg_message_1006.BG_POSITION_NR}),PRODUCT({bg_message_1006.BG_PRODUCT_DATA.Trim()}),EMPTY_TUBE_WEIGHT({bg_message_1006.BG_EMPTY_TUBE_WEIGHT})");
        //            }
        //        }
        //        else
        //        {
        //            log.Information($"MSG {l2_message.ID_MSG} - PRG {l2_message.PR_MSG} - PLC {l2_message.ID_PLC} - SRC {l2_message.ID_SRC}");
        //        }
        //    }
        //}

    }
}
