using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;


namespace libGenerator
{

    public class LogMessageEventArgs : EventArgs
    {
        public LogMessageEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; set; }
    }

    interface ICalibrator
    {
        double this[double f] { get; }
        event EventHandler<LogMessageEventArgs> Log;
    }

    public class Calibrator2 : ICalibrator
    {
        readonly List<double> ampCorrection = new List<double>();
        readonly double BandBorder;

        double FreqStepInLowBand;
        double FreqStepInHighBand;

        public double this[double f] => GetAmp(f);

        public event EventHandler<LogMessageEventArgs> Log = delegate { };

        void OnLog(string message)
        {
            Log(this, new LogMessageEventArgs(message));
        }

        public Calibrator2(double bandBorder_fHz,string calibration)
        {
            BandBorder = bandBorder_fHz;
            OnLog($"Band border {BandBorder} Hz");

            Load(calibration);

            OnLog($"Frequency step in low band is {FreqStepInLowBand}");
            OnLog($"Frequency step in high band is {FreqStepInHighBand}");
        }

        double GetAmp(double f)
        {
            if (double.IsNaN(f))
                return ampCorrection[1];

            int ind;
            if (f < BandBorder)
                ind = Round(f / FreqStepInLowBand) + 1;
            else
                ind = Round(BandBorder / FreqStepInLowBand) + Round((f - BandBorder) / FreqStepInHighBand) + 1;
            return ampCorrection[ind];
        }

        private int Round(double val)
        {
            return (int)Math.Round(val);
        }
        private double GetAmpV(double P_dBm)
        {
            return Math.Pow(10, ((P_dBm + 30 + 16.99) / 20) - 3) * Math.Sqrt(2);
        }

        private double ampMax = double.NegativeInfinity;


        private double ReadLine(string value)
        {
            var line = value.Split('\t');
            if (line.Length < 2) return double.NaN;
            var f = double.Parse(line[0], NumberStyles.Any, CultureInfo.InvariantCulture);
            var P_dBm = double.Parse(line[1], NumberStyles.Any, CultureInfo.InvariantCulture);
            var amp_V = GetAmpV(P_dBm);
            ampCorrection.Add(amp_V);

            if (amp_V > ampMax)
                ampMax = amp_V;

            return f;
        }

         void Load(string calibration)
        {
            var lines = calibration.Split('\n');
            ampCorrection.Clear();
            ampCorrection.Capacity = lines.Length;

            var f1 = ReadLine(lines[0]);
            var f2 = ReadLine(lines[1]);

            FreqStepInLowBand = (f2 - f1) * 1e6;
            f1 = f2;

            var stepPtr = FreqStepInLowBand;
            for(int i=2;i<lines.Length;i++)
            //foreach (var line in lines.Skip(2))
            {
                var line = lines[i];
                f2 = ReadLine(line);
                var step = (f2 - f1) * 1e6;
                if (Math.Abs(step - stepPtr) > 60e3)
                {
                    bool err = (f2 * 1e6 < BandBorder) || (f1 * 1e6 > BandBorder);
                    if (err)
                        //Log("Ошибка при загрузки калибровочной характиристики. Не правильная структура файла");
                        OnLog("Error on loading calibration. Invalid data structure");
                    FreqStepInHighBand = step;
                    stepPtr = step;
                }
                f1 = f2;
            }
        }

    }


    public class Calibrator
    {
        public delegate void MessageHandler(string msg);
        public event MessageHandler Log = delegate { };

        List<double> ampCorrection = new List<double>();

        public double FreqStepInLowBand { get; private set; }
        public double FreqStepInHighBand { get; private set; }
        public double BandBorder { get; private set; }

        public Calibrator(double bandBorder_fHz=double.NaN)
        {
            BandBorder = bandBorder_fHz;
        }

        public double GetAmp(double f)
        {
            if (double.IsNaN(f))
                return ampCorrection[1];

            int ind;
            if (f < BandBorder)
                ind = Round(f / FreqStepInLowBand) + 1;
            else
                ind = Round(BandBorder / FreqStepInLowBand) + Round((f - BandBorder) / FreqStepInHighBand) + 1;
            return ampCorrection[ind];
        }

        private int Round(double val)
        {
            return (int)Math.Round(val);
        }
        private double GetAmpV(double P_dBm)
        {
            return Math.Pow(10, ((P_dBm + 30 + 16.99) / 20) - 3) * Math.Sqrt(2);
        }

        private double ampMax = double.NegativeInfinity;

        private double ReadLine(string value)
        {
            var line = value.Split('\t');
            if (line.Length < 2) return double.NaN;
            var f = double.Parse(line[0], NumberStyles.Any, CultureInfo.InvariantCulture);
            var P_dBm = double.Parse(line[1], NumberStyles.Any, CultureInfo.InvariantCulture);
            var amp_V = GetAmpV(P_dBm);
            ampCorrection.Add(amp_V);

            if (amp_V > ampMax)
                ampMax = amp_V;

            return f;
        }

        public void Load(string calibration)
        {
            var lines = calibration.Split('\n');
            ampCorrection.Clear();
            ampCorrection.Capacity = lines.Length;

            var f1=ReadLine(lines[0]);
            var f2 = ReadLine(lines[1]);

            FreqStepInLowBand = (f2 - f1) * 1e6;
            f1 = f2;

            var stepPtr = FreqStepInLowBand;
            for(int i=2;i<lines.Length;i++)
            //foreach(var line in lines.Skip(2))
            {
                var line = lines[i];
                f2=ReadLine(line);
                var step = (f2 - f1) * 1e6;
                if(Math.Abs(step-stepPtr)>60e3)
                {
                    bool err = (f2 * 1e6 < BandBorder) || (f1 * 1e6 > BandBorder);
                    if (err)
                        //Log("Ошибка при загрузки калибровочной характиристики. Не правильная структура файла");
                        Log("Error on loading calibration. Invalid data structure");
                    FreqStepInHighBand = step;
                    stepPtr = step ;
                }
                f1 = f2;
            }
            Log($"Frequency step in low band is {FreqStepInLowBand}");
            Log($"Frequency step in high band is {FreqStepInHighBand}");
        }
    }
}
