using UnityEngine;

namespace TouchScript
{
    internal static class DisplayDevice
    {
        public static readonly float DotsPerCentimeter;
        private static readonly float screenTransformPixelThresholdSquared;
        private static readonly float minScreenPointsPixelDistanceSquared;

        static DisplayDevice()
        {
            const float cmToInch = 0.393700787f;
            DotsPerCentimeter = cmToInch * Screen.dpi;

            var screenTransformPixelThreshold = 0.1f * DotsPerCentimeter;
            screenTransformPixelThresholdSquared = screenTransformPixelThreshold * screenTransformPixelThreshold;

            var minScreenPointsPixelDistance = 0.5f * DotsPerCentimeter;
            minScreenPointsPixelDistanceSquared = minScreenPointsPixelDistance * minScreenPointsPixelDistance;
        }

        public static bool CheckScreenTransformPixelThreshold(Vector2 vec)
        {
            return vec.sqrMagnitude > screenTransformPixelThresholdSquared;
        }

        public static bool CheckScreenTransformPixelThreshold(float scalar)
        {
            return scalar * scalar > screenTransformPixelThresholdSquared;
        }

        public static bool CheckScreenPointsDistance(Vector2 vec)
        {
            return vec.sqrMagnitude > minScreenPointsPixelDistanceSquared;
        }
    }
}