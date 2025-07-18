﻿/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using TouchScript.Gestures.TransformGestures.Base;
using TouchScript.Utils;
using UnityEngine;

namespace TouchScript.Gestures.TransformGestures
{
    /// <summary>
    /// Recognizes a transform gesture in screen space, i.e. translation, rotation, scaling or a combination of these.
    /// </summary>
    [AddComponentMenu("TouchScript/Gestures/Screen Transform Gesture")]
    [HelpURL("http://touchscript.github.io/docs/html/T_TouchScript_Gestures_TransformGestures_ScreenTransformGesture.htm")]
    public class ScreenTransformGesture : TwoPointTransformGestureBase
    {
        #region Protected methods

        /// <inheritdoc />
        protected override float doRotation(Vector2 oldScreenPos1, Vector2 oldScreenPos2, Vector2 newScreenPos1, Vector2 newScreenPos2, Camera camera)
        {
            var oldScreenDelta = oldScreenPos2 - oldScreenPos1;
            var newScreenDelta = newScreenPos2 - newScreenPos1;
            return (Mathf.Atan2(newScreenDelta.y, newScreenDelta.x) -
                    Mathf.Atan2(oldScreenDelta.y, oldScreenDelta.x)) * Mathf.Rad2Deg;
        }

        /// <inheritdoc />
        protected override float doScaling(Vector2 oldScreenPos1, Vector2 oldScreenPos2, Vector2 newScreenPos1, Vector2 newScreenPos2, Camera camera)
        {
            return (newScreenPos2 - newScreenPos1).magnitude / (oldScreenPos2 - oldScreenPos1).magnitude;
        }

        /// <inheritdoc />
        protected override Vector3 doOnePointTranslation(Vector2 oldScreenPos, Vector2 newScreenPos, Camera projectionParams)
        {
            if (isTransforming)
            {
                return new Vector3(newScreenPos.x - oldScreenPos.x, newScreenPos.y - oldScreenPos.y, 0);
            }

            screenPixelTranslationBuffer += newScreenPos - oldScreenPos;
            if (DisplayDevice.CheckScreenTransformPixelThreshold(screenPixelTranslationBuffer))
            {
                isTransforming = true;
                return screenPixelTranslationBuffer;
            }

            return Vector3.zero;
        }

        /// <inheritdoc />
        protected override Vector3 doTwoPointTranslation(Vector2 oldScreenPos1, Vector2 oldScreenPos2,
                                                         Vector2 newScreenPos1, Vector2 newScreenPos2, float dR, float dS, Camera camera)
        {
            if (isTransforming)
            {
                var transformedPoint = scaleAndRotate(oldScreenPos1, (oldScreenPos1 + oldScreenPos2) * .5f, dR, dS);
                return new Vector3(newScreenPos1.x - transformedPoint.x, newScreenPos1.y - transformedPoint.y, 0);
            }

            screenPixelTranslationBuffer += newScreenPos1 - oldScreenPos1;
            if (DisplayDevice.CheckScreenTransformPixelThreshold(screenPixelTranslationBuffer))
            {
                isTransforming = true;
                oldScreenPos1 = newScreenPos1 - screenPixelTranslationBuffer;
                var transformedPoint = scaleAndRotate(oldScreenPos1, (oldScreenPos1 + oldScreenPos2) * .5f, dR, dS);
                return new Vector3(newScreenPos1.x - transformedPoint.x, newScreenPos1.y - transformedPoint.y, 0);
            }

            return Vector3.zero;
        }

        #endregion

        #region Private functions

        private Vector2 scaleAndRotate(Vector2 point, Vector2 center, float dR, float dS)
        {
            var delta = point - center;
            if (dR != 0) delta = TwoD.Rotate(delta, dR);
            if (dS != 0) delta = delta * dS;
            return center + delta;
        }

        #endregion
    }
}