using UnityEngine;

namespace TouchScript.Devices.Display
{
    static class DisplayDevice
    {
        public static readonly float DotsPerCentimeter;

        static DisplayDevice()
        {
            const float cmToInch = 0.393700787f;
            DotsPerCentimeter = cmToInch * Screen.dpi;
        }
    }
}