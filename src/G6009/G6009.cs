using System;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using libGenerator.Properties;

namespace libGenerator
{
    public class G6009 : Generator
    {
        Syntheziser6009 syntheziser = new Syntheziser6009();
        byte[] response = new Response6009();

        private SynthLevel synthLevel;

        public G6009() :
            base(
            vid: 0x0403,
            pid: 0x6001,
            lowestFreq: 9e3,
            highestFreq: 6e9,
            attenMin:0,
            attenMax: 63.5,
            attenStep: 0.25,
            calibratorBandBorder: 25e6)
        {
            calibrator.Load(Resources.calibrationG6009);
            synthLevel = SynthLevel.SynthLevelP5;
        }

        private bool SetAtten(double att)
        {
            //if (att > AttenuationMax) att = AttenuationMax;
            //if (att < AttenuationMin) att = AttenuationMin;
            att = att.LimitToRange(AttenuationMin, AttenuationMax);

            att = Math.Round(att / AttenuationStep) * AttenuationStep;

            CurrentAmplitude = att;
            OnAmplitudeChanged(CurrentAmplitude);

            byte halfRange = (byte)(AttenuationMax * 2);
            byte att4 = (byte)(att * 4);

            Attenuator6009 attenuator1 = new Attenuator6009();
            Attenuator6009 attenuator2 = new Attenuator6009();
            attenuator1.id = 0;
            attenuator2.id = 1;

            attenuator1.data = att4;
            if (attenuator1.data > halfRange)
                attenuator1.data = halfRange;

            attenuator2.data = (byte)(att4 - attenuator1.data);

            if (!WriteToSerial(attenuator1, true))
            {
                OnLogMessage("Cant set attenuation1");
                return false;
            }
            if (!WriteToSerial(attenuator2, true))
            {
                OnLogMessage("Cant set attenuation2");
                return false;
            }
            return true;
        }

        public bool SetAttenuation(double db, LevelControlMode mode = LevelControlMode.Attenuation)
        {
            if (db < 0) db = 0;

            switch (mode)
            {
                case LevelControlMode.Attenuation:
                    return SetAtten(db);

                case LevelControlMode.Amplitude:
                    var maxAmp = calibrator.GetAmp(CurrentFrequency);
                    if (db > maxAmp) db = maxAmp;
                    var att = 20 * Math.Log10(maxAmp / db);
                    db = maxAmp / Math.Pow(10, att / 20);
                    return SetAtten(db);

                default: return false;
            }
        }

        private bool Commute(SwitcherState state)
        {
            Switcher6009 switcher = new Switcher6009();
            if (IsActive)
                switcher.value = (byte)state;
            else
                switcher.value = 2;

            if (!WriteToSerial(switcher, true))
            {
                OnLogMessage("Switcher error");
                return false;
            }
            return true;
        }

        public  bool TurnOn(bool _on)
        {
            double fMHz = CurrentFrequency / 1e6; 

            if (_on)
            {
                if (fMHz <= 25)
                {
                    syntheziser.data[6] = 0x45;
                    syntheziser.data[7] = 0xDC;
                }
                else
                {
                    syntheziser.data[6] = 0x44;
                    syntheziser.data[7] = (byte)synthLevel;
                }
            }
            else
            {
                syntheziser.data[6] = 0x44;
                syntheziser.data[7] = 0xDC;
            }

            if (!WriteToSerial(syntheziser, true))
            {
                OnLogMessage("Generator ON false! ");
                return false;
            }
            IsActive = _on;
            var status = _on ? "ON" : "OFF";
            OnLogMessage("Generator is  " + status);
            return true;
        }

        private bool SetDDS(int buffer)
        {
            Dds6009 dds = new Dds6009();
            for (int kk = dds.freq.Length - 1; kk >= 0; --kk)
            {
                dds.freq[kk] = (byte)buffer;
                buffer = buffer >> 8;
            }

            if (!WriteToSerial(dds, true))
            {
                OnLogMessage("dds error");
                return false;
            }
            return true;
        }


        public  bool SetFrequency(double fHz) // Hz
        {
            //if (fHz < LowestFreq) fHz = LowestFreq;
            //if (fHz > HighestFreq) fHz = HighestFreq;
            fHz = fHz.LimitToRange(LowestFreq, HighestFreq);

            CurrentFrequency = fHz;
            OnFrequencyChanged(CurrentFrequency);

            var fMHz = fHz / 1e6;

            double fSynthMHz = fMHz;

            if(fMHz<=25)
            {
                fSynthMHz = 100;
                var fDds = (int)Math.Floor(Math.Pow(2, 32) * (fMHz / 100));
                SetDDS(fDds);
                fHz = fDds / Math.Pow(2, 32) * 100 * 1e6;
            }

            int k = (int)Log2(6000 / fSynthMHz);
            int n = 0x8F + (k << 4);
            syntheziser.data[5] = (byte)n;
            var tmp1 = Math.Pow(2, k) * fSynthMHz;
            var tmp2 = (int)(tmp1 / 25);
            var tmp3 = (int)Math.Round((tmp1 % 25) * 100);
            var f_Code = (tmp2 << 15) + (tmp3 << 3);
            for (int i = syntheziser.freq.Length - 1; i >= 0; --i)
            {
                syntheziser.freq[i] = (byte)f_Code;
                f_Code = f_Code >> 8;
            }

            if (fMHz <= 25)
            {
               
            }
            else
            {
                fHz = (tmp2 * 25 / Math.Pow(2, k) + tmp3 / 100 / Math.Pow(2, k)) * 1e6;
            }

            fMHz = fHz / 1e6;


            if(fMHz<=25)
            {
                Commute(SwitcherState.LowFrequency);
            }
            else
            {
                Commute(SwitcherState.HighFrequency);
            }


            // TurnOn write syntheziser to generator
            if (!TurnOn(IsActive))
            {
                OnLogMessage("syntheziser error");
                return false;
            }
            SetAtten(CurrentAmplitude);
            return true;
        }




        private bool WriteToSerial(byte[] value, bool checkResponse = false)
        {
            bool writed = WriteToPort(value);

            bool rez = checkResponse ? CheckResponse() : true;

            return writed && rez;
        }

        private bool CheckResponse()
        {
            var rez = false;
            if (IsConnected)
            {
                while (serialPort.BytesToRead < response.Length)
                {
                    Thread.Sleep(100);
                }
                var tmp = new byte[response.Length];
                serialPort.Read(tmp, 0, tmp.Length);
                if (tmp.SequenceEqual(response))
                {
                    rez = true;
                }

                serialPort.ReadExisting();
            }
            return rez;
        }
    }
}