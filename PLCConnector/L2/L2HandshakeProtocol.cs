using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace PLCConnector.L2
{
    public static class L2HandshakeProtocol
    {

        public const int L2_DB_SIZE_DINT = 1000;
        public const int DINT_SIZE = 4;

        public const int L2_MESASGE_HEADER_SIZE = 5;
        public const int L2_MESASGE_FOOTER_SIZE = 1;
        public const int MIN_L2_MESSAGE_SIZE = L2_MESASGE_HEADER_SIZE + L2_MESASGE_FOOTER_SIZE;

        static readonly string L2_FIFO_OUT_WORD_NAME = "FIFO_DW";
        static readonly string L2_FIFO_OUT_POS_WRITE_NAME = "POS_WRITE";

        static readonly string L2_FIFO_OUT_READ_DATA_NAME = "READ";

        public static readonly string GENERIC_MESSAGE_DATA_NAME = "DATA";

        public static int PROGRESSIVE_COUNTER = 100;

        public static int GetNextProgressive()
        {
            return (PROGRESSIVE_COUNTER++);
        }

        public static bool CheckMessageCorrectness(int id_plc, int id_src, int id_msg, GenericL2Message message)
        {
            return
                (message.ID_MSG == id_msg) &&
                (message.PR_MSG != 0 || true) && // Skip this test
                (message.ID_PLC == id_plc) &&
                (message.ID_SRC == id_src) &&
                (message.MSG_LEN == message.Fields.Count) &&
                (message.FOOTER == message.Fields.Count);
        }

        public static DataBlock GenerateL2MessageDescriptor(IEnumerable<string> fields_names)
        {
            var fields = new List<DataField>();
            int offset = 0;

            foreach (var name in fields_names)
            {
                fields.Add(new DataField(name, "dint", new FieldOffset(offset, 0), 0));
                offset += 4;
            }

            return new DataBlock(fields);
        }

        public static DataBlock GenerateL2GenericMessageDescriptor(int MSG_LEN, int PR_MSG, int ID_MSG, int ID_PLC, int ID_SRC, int FOOTER, List<int> data_values)
        {
            var descriptor = GetL2MessageFromDataFields(GetRepeatedFieldNames(GENERIC_MESSAGE_DATA_NAME, data_values.Count()));

            descriptor[0].Value = MSG_LEN;
            descriptor[1].Value = PR_MSG;
            descriptor[2].Value = ID_MSG;
            descriptor[3].Value = ID_PLC;
            descriptor[4].Value = ID_SRC;

            for (int i = 0; i < data_values.Count(); i++)
            {
                descriptor[i + 5].Value = data_values[i];
            }

            descriptor[MSG_LEN - 1].Value = FOOTER;

            return descriptor;
        }

        public static DataBlock GetL2MessageFromDataFields(IEnumerable<string> fields_names)
        {
            var fields = new List<string>
            {
                "MSG_LEN",
                "PR_MSG",
                "ID_MSG",
                "ID_PLC",
                "ID_SRC",
            };

            fields.AddRange(fields_names);
            fields.Add("FOOTER");

            return GenerateL2MessageDescriptor(fields);
        }

        public static IEnumerable<string> GetRepeatedFieldNames(string base_name, int count)
        {
            for (int i = 1; i <= count; i++)
            {
                yield return GetRepeatedFieldNameByIndex(base_name, i);
            }
        }

        public static string GetRepeatedFieldNameByIndex(string base_name, int index)
        {
            return $"{base_name}_{index}";
        }

        public static DataBlock GetL2DBDescriptor()
        {
            List<string> fields_names = new List<string>
            {
                L2_FIFO_OUT_POS_WRITE_NAME
            };

            fields_names.AddRange(GetRepeatedFieldNames(L2_FIFO_OUT_WORD_NAME, (L2_DB_SIZE_DINT - 1)));

            return GenerateL2MessageDescriptor(fields_names);
        }

        public static DataBlock GetL2HandshakeResponseDescriptor()
        {
            var fields_names = new List<string>()
            {
                L2_FIFO_OUT_READ_DATA_NAME
            };

            var response = GenerateL2MessageDescriptor(fields_names);

            return response;
        }

        public static DataBlock GetL2HandshakeResponse(int total_words_read)
        {
            var response = GetL2HandshakeResponseDescriptor();
            response.Fields[0].Value = total_words_read;
            return response;
        }

        public static IEnumerable<DataBlock> GetMessagesFromL2DB(DataBlock l2_db, out int total_words_read)
        {
            var pos_write = l2_db[L2_FIFO_OUT_POS_WRITE_NAME].As<int>();

            // First message's msg_len
            var current_index = 1;

            var MSG_LEN = l2_db[current_index].As<int>();

            total_words_read = 0;

            var messages = new List<DataBlock>();

            while (MSG_LEN > 0 && current_index <= pos_write)
            {
                if (MSG_LEN < MIN_L2_MESSAGE_SIZE)
                    throw new FormatException($"Message at index {current_index} with MSG_LEN less than allowed size");

                if (MSG_LEN > (L2_DB_SIZE_DINT - current_index))
                    throw new FormatException($"Message at index {current_index} with MSG_LEN exceeding remaining words");

                var PR_MSG = l2_db[current_index + 1].As<int>();
                var ID_MSG = l2_db[current_index + 2].As<int>();
                var ID_PLC = l2_db[current_index + 3].As<int>();
                var ID_SRC = l2_db[current_index + 4].As<int>();

                var total_data_words = MSG_LEN - MIN_L2_MESSAGE_SIZE;

                var data_values = new List<int>();

                for (int i = 0; i < total_data_words; i++)
                {
                    var data_field_value = l2_db[current_index + 5 + i].As<int>();
                    data_values.Add(data_field_value);
                }

                var FOOTER = l2_db[current_index + MSG_LEN - 1].As<int>();

                if (FOOTER != MSG_LEN)
                    throw new FormatException($"Message at index {current_index} with MSG_LEN <> FOOTER");

                current_index += MSG_LEN;

                if (current_index > (L2_DB_SIZE_DINT - 1))
                    break;

                messages.Add(GenerateL2GenericMessageDescriptor(MSG_LEN, PR_MSG, ID_MSG, ID_PLC, ID_SRC, FOOTER, data_values));

                total_words_read += MSG_LEN;
                MSG_LEN = l2_db[current_index].As<int>();
            }

            if (pos_write != total_words_read)
                throw new FormatException($"POS_WRITE doesn't match the number of total words read");

            return messages;
        }

    }
}
