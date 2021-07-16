using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace libGenerator
{
    public enum SynthLevel  :byte
    {
        Off = 0xDC,
        SynthLevelM4 = 0xE4,
        SynthLevelM1 = 0xEC,
        SynthLevelP2 = 0xF4,
        SynthLevelP5 = 0xFC
    };
    public enum LevelControlMode
    {
        Amplitude,
        Attenuation
    };
    public enum SwitcherState
    {
        LowFrequency = 1,
        HighFrequency = 0
    };
}
