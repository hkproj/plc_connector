using CsvHelper;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;

namespace PLCConnector
{

    public class DataBlock
    {

        public DataBlock(DataBlock other)
        {
            this.Fields = other.Fields;
        }

        public DataBlock(IEnumerable<DataField> fields)
        {
            // Check for duplicate names
            if (fields.Select(f => f.Name).Distinct().Count() != fields.Count())
                throw new ArgumentException("Fields must have unique names");

            this.Fields = fields.OrderBy(c => c.Offset, new FieldOffsetComparer()).ToList();
        }

        public static IEnumerable<DataField> ParseDescription(string csv_content)
        {
            using var reader = new StringReader(csv_content);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            while (csv.Read())
            {
                yield return new DataField(csv.GetField(0), csv.GetField(1), new FieldOffset(int.Parse(csv.GetField(2)), int.Parse(csv.GetField(3))));
            }
        }

        public IReadOnlyList<DataField> Fields { get; private set; }

        public void ShiftAllByOffset(int shift)
        {
            foreach (var field in Fields)
                field.Offset = new FieldOffset(field.Offset.Byte, field.Offset.Bit, shift);
        }

        public DataField this[string name]
        {
            get
            {
                return Fields.SingleOrDefault(f => f.Name == FieldName(name));
            }
        }

        public DataField this[int index]
        {
            get
            {
                return Fields.ElementAt(index);
            }
        }

        public static string FieldName(string name)
        {
            return name.ToUpper();
        }

    }

    public struct FieldOffset
    {

        public FieldOffset(int @byte, int bit, int shift = 0)
        {
            Shift = shift;
            Bit = bit;
            _Byte = @byte;
        }

        public readonly int _Byte;

        public int Byte
        {
            get => _Byte + Shift;
        }

        public readonly int Bit;

        public readonly int Shift;

        public override string ToString()
        {
            return $"{Byte}.{Bit}";
        }

    }

    public class FieldOffsetComparer : IComparer<FieldOffset>
    {

        public int Compare(FieldOffset x, FieldOffset y)
        {
            if (x.Byte > y.Byte || (x.Byte == y.Byte && x.Bit > y.Bit))
                return 1;
            else if (x.Byte == y.Byte && x.Bit == y.Bit)
                return 0;
            else
                return -1;
        }

    }

    public class DataField
    {

        public DataField(string name, string type, FieldOffset offset)
        {
            Initialize(name, type, offset, null);
        }

        public DataField(string name, string type, FieldOffset offset, object value)
        {
            Initialize(name, type, offset, value);
        }

        void Initialize(string name, string type, FieldOffset offset, object value)
        {
            this.Name = DataBlock.FieldName(name);
            this.DataType = type;
            this.Offset = offset;
            this.Value = value;
        }

        public string Name { get; set; }

        public string DataType { get; set; }

        public FieldOffset Offset { get; set; }

        public object Value { get; set; }

        public T As<T>()
        {
            return (T)Value;
        }

        public override string ToString()
        {
            return Name;
        }

    }

}
