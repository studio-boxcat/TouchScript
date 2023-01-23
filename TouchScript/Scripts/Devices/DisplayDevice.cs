/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using UnityEngine;

namespace TouchScript.Devices.Display
{
    public static class DisplayDevice
    {
        public static readonly float DPI;
        public static readonly float DotsPerCentimeter;

        /// <summary>
        /// Centimeter to inch ratio to be used in DPI calculations.
        /// </summary>
        public const float CM_TO_INCH = 0.393700787f;

        static DisplayDevice()
        {
            DPI = Screen.dpi;
            DotsPerCentimeter = CM_TO_INCH * DPI;
        }
    }
}