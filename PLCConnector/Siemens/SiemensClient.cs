using Sharp7;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace PLCConnector.Siemens
{
    public class SiemensClient
    {

        public SiemensClient(string address, int rack = 0, int slot = 0)
        {
            this.Address = address;
            this.Rack = rack;
            this.Slot = slot;
        }

        public string Address { get; set; }

        public int Rack { get; set; }

        public int Slot { get; set; }

        public byte[] ReadRawData(int db_number, int offset, int length)
        {
            if (offset < 0)
                throw new IndexOutOfRangeException("Offset can't be negative");

            if (length <= 0)
                throw new IndexOutOfRangeException("Length must be greater than zero");

            if (db_number <= 0)
                throw new IndexOutOfRangeException("DB Number must be greater than zero");

            var buffer = new byte[length];
            var client = new S7Client();

            var retval = client.ConnectTo(Address, Rack, Slot);
            
            if (retval != 0)
                throw new ClientException($"Could not connect to the PLC: {client.ErrorText(retval)}");

            retval = client.ReadArea(S7Consts.S7AreaDB, db_number, offset, length, S7Consts.S7WLByte, buffer);

            if (retval != 0)
                throw new ClientException($"Could not retrieve data from DB {db_number}: {client.ErrorText(retval)}");

            return buffer;
        }

        public void WriteRawData(int db_number, int offset, int length, byte[] buffer)
        {
            if (offset < 0)
                throw new IndexOutOfRangeException("Offset can't be negative");

            if (length <= 0)
                throw new IndexOutOfRangeException("Length must be greater than zero");

            if (db_number <= 0)
                throw new IndexOutOfRangeException("DB Number must be greater than zero");

            if (buffer.Length < (length + offset))
                throw new IndexOutOfRangeException("Buffer not large enough");

            var client = new S7Client();

            var retval = client.ConnectTo(Address, Rack, Slot);

            if (retval != 0)
                throw new ClientException($"Could not connect to the PLC: {client.ErrorText(retval)}");

            retval = client.WriteArea(S7Consts.S7AreaDB, db_number, offset, length, S7Consts.S7WLByte, buffer);

            if (retval != 0)
                throw new ClientException($"Could not write data to DB {db_number}: {client.ErrorText(retval)}");
        }

        public DataBlock ReadData(byte[] buffer, ref DataBlock data_block)
        {
            foreach (var field in data_block.Fields)
            {
                field.Value = field.DataType switch
                {
                    "bool" => (bool)S7.GetBitAt(buffer, field.Offset.Byte, field.Offset.Bit),
                    "byte" => (byte)S7.GetByteAt(buffer, field.Offset.Byte),
                    "sint" => (int)S7.GetSIntAt(buffer, field.Offset.Byte),
                    "int" => (short)S7.GetIntAt(buffer, field.Offset.Byte),
                    "dint" => (int)S7.GetDIntAt(buffer, field.Offset.Byte),
                    "lint" => (long)S7.GetLIntAt(buffer, field.Offset.Byte),
                    "usint" => (byte)S7.GetUSIntAt(buffer, field.Offset.Byte),
                    "uint" => (ushort)S7.GetUIntAt(buffer, field.Offset.Byte),
                    "udint" => (uint)S7.GetUDIntAt(buffer, field.Offset.Byte),
                    "ulint" => (ulong)S7.GetULIntAt(buffer, field.Offset.Byte),
                    "word" => (ushort)S7.GetWordAt(buffer, field.Offset.Byte),
                    "dword" => (uint)S7.GetDWordAt(buffer, field.Offset.Byte),
                    "lword" => (ulong)S7.GetLWordAt(buffer, field.Offset.Byte),
                    "real" => (float)S7.GetRealAt(buffer, field.Offset.Byte),
                    "lreal" => (double)S7.GetLRealAt(buffer, field.Offset.Byte),
                    "s7_date_and_time" => (DateTime)S7.GetDateTimeAt(buffer, field.Offset.Byte),
                    "s7_date" => (DateTime)S7.GetDateAt(buffer, field.Offset.Byte),
                    "s7_time_of_day" => (DateTime)S7.GetTODAt(buffer, field.Offset.Byte),
                    "s7_1500_long_time_of_day" => (DateTime)S7.GetLTODAt(buffer, field.Offset.Byte),
                    "s7_1500_long_date_and_time" => (DateTime)S7.GetLDTAt(buffer, field.Offset.Byte),
                    "s7_1500_date_and_time" => (DateTime)S7.GetDTLAt(buffer, field.Offset.Byte),
                    "s7_string" => (string)S7.GetStringAt(buffer, field.Offset.Byte),
                    "s7_wstring" => (string)S7.GetWStringAt(buffer, field.Offset.Byte),
                    _ => throw new NotImplementedException($"Reader not implemented for this data type: {field.DataType}"),
                };
            }

            return data_block;
        }

        public byte[] WriteData(ref byte[] buffer, DataBlock data_block)
        {
            foreach (var field in data_block.Fields)
            {
                switch(field.DataType)
                {
                    case "bool":
                        S7.SetBitAt(ref buffer, field.Offset.Byte, field.Offset.Bit, (bool)field.Value);
                        break;
                    case "byte":
                        S7.SetByteAt(buffer, field.Offset.Byte, (byte)field.Value);
                        break;
                    case "sint":
                        S7.SetSIntAt(buffer, field.Offset.Byte, (int)field.Value);
                        break;
                    case "int":
                        S7.SetIntAt(buffer, field.Offset.Byte, (short)field.Value);
                        break;
                    case "dint":
                        S7.SetDIntAt(buffer, field.Offset.Byte, (int)field.Value);
                        break;
                    case "lint": 
                        S7.SetLIntAt(buffer, field.Offset.Byte, (long)field.Value);
                        break;
                    case "usint":
                        S7.SetUSIntAt(buffer, field.Offset.Byte, (byte)field.Value);
                        break;
                    case "uint":
                        S7.SetUIntAt(buffer, field.Offset.Byte, (ushort)field.Value);
                        break;
                    case "udint":
                        S7.SetUDIntAt(buffer, field.Offset.Byte, (uint)field.Value);
                        break;
                    case "ulint":
                        S7.SetULintAt(buffer, field.Offset.Byte, (ulong)field.Value);
                        break;
                    case "word":
                        S7.SetWordAt(buffer, field.Offset.Byte, (ushort)field.Value);
                        break;
                    case "dword":
                        S7.SetDWordAt(buffer, field.Offset.Byte, (uint)field.Value);
                        break;
                    case "lword":
                        S7.SetLWordAt(buffer, field.Offset.Byte, (ulong)field.Value);
                        break;
                    case "real":
                        S7.SetRealAt(buffer, field.Offset.Byte, (float)field.Value);
                        break;
                    case "lreal":
                        S7.SetLRealAt(buffer, field.Offset.Byte, (double)field.Value);
                        break;
                    case "s7_date_and_time":
                        S7.SetDateTimeAt(buffer, field.Offset.Byte, (DateTime)field.Value);
                        break;
                    case "s7_date": 
                        S7.SetDateAt(buffer, field.Offset.Byte, (DateTime)field.Value);
                        break;
                    case "s7_time_of_day": 
                        S7.SetTODAt(buffer, field.Offset.Byte, (DateTime)field.Value);
                        break;
                    case "s7_1500_long_time_of_day":
                        S7.SetLTODAt(buffer, field.Offset.Byte, (DateTime)field.Value);
                        break;
                    case "s7_1500_long_date_and_time": 
                        S7.SetLDTAt(buffer, field.Offset.Byte, (DateTime)field.Value);
                        break;
                    case "s7_1500_date_and_time": 
                        S7.SetDTLAt(buffer, field.Offset.Byte, (DateTime)field.Value);
                        break;
                    case "s7_string": 
                        S7.SetStringAt(buffer, field.Offset.Byte, ((string)field.Value).Length, (string)field.Value);
                        break;
                    case "s7_wstring": 
                        S7.SetWStringAt(buffer, field.Offset.Byte, ((string)field.Value).Length, (string)field.Value);
                        break;
                    default:
                        throw new NotImplementedException($"Writer not implemented for this data type: {field.DataType}");
                };
            }

            return buffer;
        }

        public DataBlock ReadData(int db_number, int offset, int length, ref DataBlock data_block)
        {
            var bytes = ReadRawData(db_number, offset, length);
            return ReadData(bytes, ref data_block);
        }

    }
}
