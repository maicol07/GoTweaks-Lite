using System.Collections.Generic;
using System.Drawing;
using XboxGamingBarHelper.Performance;

namespace XboxGamingBarHelper.RTSS.OSDItems
{
    internal class OSDItemCPU : OSDItem
    {
        private HardwareSensor cpuUsageSensor;
        private HardwareSensor cpuClockSensor;
        private HardwareSensor cpuWattageSensor;
        private HardwareSensor cpuTemperatureSensor;

        private bool showClock = false;

        public OSDItemCPU(HardwareSensor cpuUsageSensor, HardwareSensor cpuClockSensor, HardwareSensor cpuWattageSensor, HardwareSensor cpuTemperatureSensor) : base("CPU", "CPU", Color.Turquoise)
        {
            this.cpuWattageSensor = cpuWattageSensor;
            this.cpuUsageSensor = cpuUsageSensor;
            this.cpuClockSensor = cpuClockSensor;
            this.cpuTemperatureSensor = cpuTemperatureSensor;
        }

        public void SetShowClock(bool show)
        {
            showClock = show;
        }

        protected override string GetModernOSDString(int osdLevel)
        {
            string value = MValueColor();
            string text = MLabel("CPU", "7ED5FF");

            float usage = cpuUsageSensor.Value;
            text += usage < 0 ? $"<C={value}>--" : MNum(usage, 2, value) + MUnit("%");

            float wattage = cpuWattageSensor.Value;
            if (wattage >= 0)
                text += $"<C={value}>  " + MNum(wattage, 1, value, oneDecimal: true) + MUnit("W");

            float temperature = cpuTemperatureSensor.Value;
            if (temperature > 0)
                text += $"<C={value}>  " + MNum(temperature, 2, MHighIsBad(temperature, 80, 90)) + MUnit("°C");

            if (showClock && cpuClockSensor.Value > 0)
            {
                text += $"<C={value}>  " + MNum(cpuClockSensor.Value / 1000.0, 1, value, oneDecimal: true) + MUnit("GHz");
            }
            return text;
        }

        protected override List<OSDItemValue> GetValues(int osdLevel)
        {
            var osdItems = base.GetValues(osdLevel);

            // Show CPU usage, wattage and temperature when enabled
            osdItems.Add(new OSDItemValue(cpuUsageSensor.Value, "%", OSDValueType.Percentage));
            osdItems.Add(new OSDItemValue(cpuWattageSensor.Value, "W", OSDValueType.Wattage));
            osdItems.Add(new OSDItemValue(cpuTemperatureSensor.Value, "C", OSDValueType.Temperature));

            // Show clock speed if enabled separately
            if (showClock)
            {
                osdItems.Add(new OSDItemValue(cpuClockSensor.Value, "MHz", OSDValueType.Speed));
            }

            return osdItems;
        }
    }
}
