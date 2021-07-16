using System;
using System.Collections.Generic;

namespace libGenerator
{
    public class Head6009
    {
        public byte[] data = { 0xA5, 0xA4, 0xA3, 0xA2, 0xA1 };

        public static implicit operator byte[](Head6009 val)
        {
            return val.data;
        }
        public int Count => ((byte[])this).Length;

    }

    public class Reset6009 
    {
        public Head6009 head = new Head6009();
        public byte data = 0xA0;

        public int Count => GetData().Length;

        public byte[] GetData()
        {
            List<byte> lst = new List<byte>();
            lst.AddRange(head.data);
            lst.Add(data);
            return lst.ToArray();
        }
    }

    public class Attenuator6009
    {
        public Head6009 head = new Head6009();
        public byte command = 0xA1;
        public byte id = 0x00;
        public byte data = 0x00;

        public static implicit operator byte[] (Attenuator6009 val)
        {
            List<byte> lst = new List<byte>();
            lst.AddRange(val.head.data);
            lst.Add(val.command);
            lst.Add(val.id);
            lst.Add(val.data);
            return lst.ToArray();
        }
        public int Count => ((byte[])this).Length;

    }

    public class Syntheziser6009
    {
        public Head6009 head = new Head6009();
        public byte command = 0xA2;
        public byte id = 0x01;
        public byte[] data = { 0x01, 0x40, 0x00, 0x05,
                               0x61, 0x00, 0x44, 0xDC,
                               0x00, 0x00, 0x00, 0x03,
                               0x00, 0x00, 0x7E, 0x42,
                               0x60, 0x00, 0xCE, 0x21 };
        public byte[] freq = { 0x00, 0x00, 0x00, 0x00 };


        public static implicit operator byte[] (Syntheziser6009 val)
        {
            List<byte> lst = new List<byte>();
            lst.AddRange(val.head.data);
            lst.Add(val.command);
            lst.Add(val.id);
            lst.AddRange(val.data);
            lst.AddRange(val.freq);
            return lst.ToArray();
        }
        public int Count => ((byte[])this).Length;
    }

    public class Dds6009 
    {
        public Head6009 head = new Head6009();
        public byte command = 0xA2;
        public byte id = 0x02;
        public byte phase = 0x00;
        public byte[] freq = { 0x00, 0x00, 0x00, 0x00 };


        public static implicit operator byte[] (Dds6009 val)
        {
            List<byte> lst = new List<byte>();
            lst.AddRange(val.head.data);
            lst.Add(val.command);
            lst.Add(val.id);
            lst.Add(val.phase);
            lst.AddRange(val.freq);
            return lst.ToArray();
        }
        public int Count => ((byte[])this).Length;
    }

    public class Switcher6009 
    {
        public Head6009 head = new Head6009();
        public byte command = 0xA4;
        public byte value = 0x00;

        public static implicit operator byte[] (Switcher6009 val)
        {
            List<byte> lst = new List<byte>();
            lst.AddRange(val.head.data);
            lst.Add(val.command);
            lst.Add(val.value);
            return lst.ToArray();
        }
        public int Count => ((byte[])this).Length;
    }
    public class Response6009 
    {
        public byte[] data= { 0xA4, 0xA3, 0xA2, 0xA1, 0xA0 };

        public static implicit operator byte[] (Response6009 val)
        {
            return val.data;
        }
        public int Count => ((byte[])this).Length;
    }


}
