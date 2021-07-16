using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace libGenerator
{
    public abstract class Generator
    {
        public delegate void StringHandler(string msg);
        public event StringHandler LogMessage = delegate { };

        public delegate void DoubleHandler(double x);
        public event DoubleHandler FrequencyChanged = delegate { };
        public event DoubleHandler AmplitudeChanged = delegate { };

        /// <summary>
        /// Vendor ID
        /// </summary>
        public int VID { get; private set; }
        /// <summary>
        /// Product ID
        /// </summary>
        public int PID { get; private set; }

        public double LowestFreq { get; private set; }
        public double HighestFreq { get; private set; }
        public double AttenuationMin { get; private set; }
        public double AttenuationMax { get; private set; }
        public double AttenuationStep { get; private set; }


        public bool IsConnected => serialPort.IsOpen;
        public bool IsActive { get;protected set; }
        /// <summary>
        /// Current frequency (Hz)
        /// </summary>
        public double CurrentFrequency { get; protected set; }
        /// <summary>
        /// Current amplitude (dB)
        /// </summary>
        public double CurrentAmplitude { get; protected set; }

        protected readonly SerialPort serialPort;
        protected readonly Calibrator calibrator;


        public Generator(int vid, int pid, double lowestFreq, double highestFreq, double attenMin, double attenMax, double attenStep, double calibratorBandBorder)
        {
            VID = vid;
            PID = pid;
            LowestFreq = lowestFreq;
            HighestFreq = highestFreq;
            AttenuationMin = attenMin;
            AttenuationMax = attenMax;
            AttenuationStep = attenStep;
            calibrator = new Calibrator(calibratorBandBorder);
            calibrator.Log += x => OnLogMessage("Calibrator: "+x);
            serialPort = new SerialPort()
            {
                BaudRate = 115200,
                Parity = Parity.None,
                DataBits = 8
            };

        }
        protected double Log2(double x)
        {
            return Math.Log10(x) / Math.Log10(2);
        }

        public bool Connect(string portName)
        {
            serialPort.PortName = portName;
            try
            {
                serialPort.Open();
            }
            catch
            {
                OnLogMessage("ERR: connect error");
                return false;
            }
            OnLogMessage($"Connected");
            return true;
        }
        public void Close()
        {
            OnLogMessage("Close");
            serialPort.Close();
        }

        protected bool WriteToPort(byte[] value)
        {
            if (!serialPort.IsOpen) return false;
            serialPort.Write(value, 0, value.Length);
            return true;
        }

        protected virtual void OnLogMessage(string msg)
        {
            LogMessage(msg);
        }
        protected virtual void OnFrequencyChanged(double f)
        {
            FrequencyChanged(f);
            OnLogMessage($"FrequencyChanged {f} Hz");
        }
        protected virtual void OnAmplitudeChanged(double f)
        {
            AmplitudeChanged(f);
            OnLogMessage($"AmplitudeChanged {f} dB");
        }

        public IEnumerable<string> GetSerialPorts() => GetSerialPorts(VID, PID);
        public static IEnumerable<string> GetSerialPorts(int vid, int pid)
        {
            var serialPortNames = SerialPort.GetPortNames();
           // return serialPortNames;

            var VID = vid.ToString("X4");
            var PID = pid.ToString("X4");

            String pattern = String.Format("^VID_{0}.PID_{1}", VID, PID);
            Regex _rx = new Regex(pattern, RegexOptions.IgnoreCase);

            List<string> comports = new List<string>();
            RegistryKey rk1 = Registry.LocalMachine;
            RegistryKey rk2 = rk1.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum");
            foreach (var s3 in rk2.GetSubKeyNames())
            {
                RegistryKey rk3 = rk2.OpenSubKey(s3);
                foreach (var s in rk3.GetSubKeyNames())
                {
                    if (_rx.Match(s).Success)
                    {
                        RegistryKey rk4 = rk3.OpenSubKey(s);
                        foreach (var s2 in rk4.GetSubKeyNames())
                        {
                            RegistryKey rk5 = rk4.OpenSubKey(s2);
                            RegistryKey rk6 = rk5.OpenSubKey("Device Parameters");
                            //comports.Add((string)rk6.GetValue("PortName"));
                            var port = (string)rk6.GetValue("PortName");
                            if (!string.IsNullOrEmpty(port))
                                comports.Add(port);
                        }
                    }
                }
            }
            var res = comports.Intersect(serialPortNames);
            return res;
        }
    }
}
