using Serilog;
using LineMap.Managers;
using Newtonsoft.Json.Linq;
using System.IO;
using Copto;

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

            var opts = Options.Parse(args);

            var device_name = "";

            opts.Apply(new RuleSet()
            {
                { "connect", (name) => device_name = name }
            });

            if (string.IsNullOrEmpty(device_name))
            {
                log.Error($"No device name was specified");
                return;
            }

            var devices = JObject.Parse(File.ReadAllText(Path.GetFullPath("devices.json")));
            var device = devices["devices"][device_name];

            if (device == null)
            {
                log.Error($"No device with name {device_name} was found");
                return;
            }

            var fifo_in_db_number = int.Parse(device["fifo_in_db_number"].Value<string>());
            var fifo_in_pos_db_number = int.Parse(device["fifo_in_pos_db_number"].Value<string>());
            var fifo_out_db_number = int.Parse(device["fifo_out_db_number"].Value<string>());
            var fifo_out_pos_db_number = int.Parse(device["fifo_out_pos_db_number"].Value<string>());
            var id_plc = int.Parse(device["id_plc"].Value<string>());
            var device_nr = int.Parse(device["device_nr"].Value<string>());
            var ip_address = device["ip_address"].Value<string>();

            if (device_name.StartsWith("sa"))
            {
                var sa_manager = new StackerCraneInteractiveManager(new DeviceConfiguration()
                {
                    FifoInDBNumber = fifo_in_db_number,
                    FifoInPosDBNumber = fifo_in_pos_db_number,
                    FifoOutDBNumber = fifo_out_db_number,
                    FifoOutPosDBNumber = fifo_out_pos_db_number,
                    IDDevice = device_nr,
                    IDPlc = id_plc,
                    IPAddress = ip_address
                }, log);
                sa_manager.Start();
            }
            else
            {
                log.Error($"No interactive manager found for device {device_name}");
            }
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
