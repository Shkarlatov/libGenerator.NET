using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace libGenerator
{
    public class SerialPortInfo
    {
        public SerialPortInfo(ushort vid, ushort pid, string portName, string description)
        {
            VID = vid;
            PID = pid;
            PortName = portName;
            Description = description;
        }

        public ushort VID { get; }
        public ushort PID { get; }

        public string PortName { get; }
        public string Description { get; }

        public SerialPort SerialPort => new SerialPort(PortName);

        public override string ToString()
        {
            return
                $"VID: {VID.ToString("X4")} " +
                $"PID: {PID.ToString("X4")} " +
                $"PortName: {PortName} " +
                $"Description: {Description} ";
        }


        public static IEnumerable<SerialPortInfo> GetBy(int? vid, int? pid)
        {
            var allPorts = GetSerialPorts().ToArray();
            if (allPorts == null)
                return null;


            if (vid.HasValue && pid.HasValue)
                return allPorts.Where(x => x.VID == vid && x.PID == pid).ToArray();

            if (vid.HasValue)
                return allPorts.Where(x => x.VID == vid).ToArray();

            if (pid.HasValue)
                return allPorts.Where(x => x.PID == pid).ToArray();

            return null;
        }

        public static IEnumerable<SerialPortInfo> GetSerialPorts()
        {
            var avaliblePorts = SerialPort.GetPortNames();

            var comNamePattern = @"\(COM\d*\)$";
            var com_regexp = new Regex(comNamePattern);

            var pattern = "^VID_[0-9A-z]*.PID_[0-9A-z]*";
            var regexp = new Regex(pattern, RegexOptions.IgnoreCase);
            var root = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum");

            foreach (var group in root.GetSubKeyNames()) // group = ACPI, FTDIBUS ... 
            {
                var group_key = root.OpenSubKey(group); // open FTDIBUS
                foreach (var hardwareid in group_key.GetSubKeyNames())  //  VID_0403+PID_6001+....
                {
                    var match = regexp.Match(hardwareid);
                    if (match.Success)
                    {
                        // get VID PID

                        var hardwareid_key = group_key.OpenSubKey(hardwareid);
                        foreach (var hardware in hardwareid_key.GetSubKeyNames())
                        {
                            var id_key = hardwareid_key.OpenSubKey(hardware);
                            // FriendlyName
                            var friendlyName = (string)id_key.GetValue("FriendlyName");
                            if (!string.IsNullOrEmpty(friendlyName))
                            {
                                var comNameMatch = com_regexp.Match(friendlyName);
                                if (comNameMatch.Success)
                                {
                                    var vid = Convert.ToUInt16(hardwareid.Substring(4, 4), 16);
                                    var pid = Convert.ToUInt16(hardwareid.Substring(13, 4), 16);

                                    var description = friendlyName.Substring(0, friendlyName.Length - comNameMatch.Length);
                                    var portname = comNameMatch.Value.Substring(1, comNameMatch.Value.Length - 2); // COM21

#if DEBUG
                                    var port = new SerialPortInfo(vid, pid, portname, description);
                                    yield return port;
#else
                                     foreach (var avaliblePort in avaliblePorts)
                                    {
                                        if (avaliblePort == portname)
                                        {
                                            var port = new SerialPortInfo(vid, pid, portname, description);
                                            yield return port;
                                        }
                                    }
#endif

                                }
                            }
                        }
                    }
                }
            }
        }

    }
}
