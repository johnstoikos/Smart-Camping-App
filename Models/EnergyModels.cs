using System;
using System.Collections.Generic;
using System.Drawing;

namespace SmartCamping.Models
{
    public enum AcMode { Off, Cool, Heat, Fan }

    public class EnergyDevice
    {
        public string Name { get; set; } = "";
        public int PowerW { get; set; }      // ονομαστική κατανάλωση
        public bool IsOn { get; set; }
    }

    public class EnergyState
    {
        public int BatteryPercent { get; set; } = 78;  
        public int PvPowerW { get; set; }              // ισχύς PV
        public int LoadPowerW { get; set; }            // κατανάλωση (συσκευές + A/C)
        public int NetPowerW { get; set; }             // PV - Load
        public double EstHoursRemaining { get; set; }  // εκτίμηση ωρών αυτονομίας
        public bool AutoSave { get; set; } = true;

        public bool AcOn { get; set; }
        public AcMode AcMode { get; set; } = AcMode.Off;
        public int AcSetpointC { get; set; } = 24;

        public string? LastAction { get; set; }

        public List<EnergyDevice> Devices { get; set; } = new List<EnergyDevice>
        {
            new EnergyDevice{ Name = "Ψυγείο",         PowerW = 60, IsOn = true },
            new EnergyDevice{ Name = "Αντλία Νερού",  PowerW = 40, IsOn = false },
            new EnergyDevice{ Name = "Φορτιστής",     PowerW = 20, IsOn = false }
        };

        public EnergyState Clone()
        {
            return new EnergyState
            {
                BatteryPercent = BatteryPercent,
                PvPowerW = PvPowerW,
                LoadPowerW = LoadPowerW,
                NetPowerW = NetPowerW,
                EstHoursRemaining = EstHoursRemaining,
                AutoSave = AutoSave,
                AcOn = AcOn,
                AcMode = AcMode,
                AcSetpointC = AcSetpointC,
                LastAction = LastAction,
                Devices = new List<EnergyDevice>(Devices.ConvertAll(d =>
                    new EnergyDevice { Name = d.Name, PowerW = d.PowerW, IsOn = d.IsOn }))
            };
        }
    }
}
