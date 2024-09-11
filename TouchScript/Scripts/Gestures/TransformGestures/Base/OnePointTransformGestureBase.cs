/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using Sirenix.OdinInspector;
using TouchScript.Pointers;
using TouchScript.Utils;
using UnityEngine;

namespace TouchScript.Gestures.TransformGestures.Base
{
    /// <summary>
    /// Abstract base class for Pinned Transform Gestures.
    /// </summary>
    public abstract class OnePointTransformGestureBase : TransformGestureBase
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        #region Public properties

        /// <inheritdoc />
        public override Vector2 ScreenPosition => activePointers.Count is not 0 ? activePointers[0].Position : InvalidPosition.Value;

        /// <inheritdoc />
        public override Vector2 PreviousScreenPosition => activePointers.Count is not 0 ? activePointers[0].PreviousPosition : InvalidPosition.Value;

        #endregion

        #region Private variables

        /// <summary>
        /// Rotation buffer.
        /// </summary>
        protected float screenPixelRotationBuffer;

        /// <summary>
        /// Angle buffer.
        /// </summary>
        protected float angleBuffer;

        /// <summary>
        /// Screen space scaling buffer.
        /// </summary>
        protected float screenPixelScalingBuffer;

        /// <summary>
        /// Scaling buffer.
        /// </summary>
        protected float scaleBuffer;

        #endregion

        #region Gesture callbacks

        /// <inheritdoc />
        protected override void pointersUpdated(IList<Pointer> pointers)
        {
            base.pointersUpdated(pointers);

            var thePointer = activePointers[0];
            var targetCamera = thePointer.GetPressData().Layer.GetTargetCamera();
            var dR = deltaRotation = 0;
            var dS = deltaScale = 1f;

            if (pointersNumState != PointersNumState.Exists) return;

            var rotationEnabled = (Type & TransformGesture.TransformType.Rotation) == TransformGesture.TransformType.Rotation;
            var scalingEnabled = (Type & TransformGesture.TransformType.Scaling) == TransformGesture.TransformType.Scaling;
            if (!rotationEnabled && !scalingEnabled) return;
            if (!relevantPointers(pointers, thePointer)) return;

            var worldCenter = cachedTransform.position;
            var screenCenter = (Vector2) targetCamera.WorldToScreenPoint(worldCenter);
            var newScreenPos = thePointer.Position;

            // Here we can't reuse last frame screen positions because points 0 and 1 can change.
            // For example if the first of 3 fingers is lifted off.
            var oldScreenPos = getPointPreviousScreenPosition();

            if (rotationEnabled)
            {
                if (isTransforming)
                {
                    dR = doRotation(worldCenter, oldScreenPos, newScreenPos, targetCamera);
                }
                else
                {
                    // Find how much we moved perpendicular to the line (center, oldScreenPos)
                    screenPixelRotationBuffer += TwoD.PointToLineDistance(screenCenter, oldScreenPos, newScreenPos);
                    angleBuffer += doRotation(worldCenter, oldScreenPos, newScreenPos, targetCamera);

                    if (DisplayDevice.CheckScreenTransformPixelThreshold(screenPixelRotationBuffer))
                    {
                        isTransforming = true;
                        dR = angleBuffer;
                    }
                }
            }

            if (scalingEnabled)
            {
                if (isTransforming)
                {
                    dS *= doScaling(worldCenter, oldScreenPos, newScreenPos, targetCamera);
                }
                else
                {
                    screenPixelScalingBuffer += (newScreenPos - screenCenter).magnitude -
                                                (oldScreenPos - screenCenter).magnitude;
                    scaleBuffer *= doScaling(worldCenter, oldScreenPos, newScreenPos, targetCamera);

                    if (DisplayDevice.CheckScreenTransformPixelThreshold(screenPixelScalingBuffer))
                    {
                        isTransforming = true;
                        dS = scaleBuffer;
                    }
                }
            }

            if (dR != 0) transformMask |= TransformGesture.TransformType.Rotation;
            if (dS != 1) transformMask |= TransformGesture.TransformType.Scaling;

            if (transformMask != 0)
            {
                if (State == GestureState.Possible) setState(GestureState.Began);
                switch (State)
                {
                    case GestureState.Began:
                    case GestureState.Changed:
                        deltaRotation = dR;
                        deltaScale = dS;
                        setState(GestureState.Changed);
                        resetValues();
                        break;
                }
            }

            return;

            static bool relevantPointers(IList<Pointer> pointers, Pointer activePointer)
            {
                // We care only about the first pointer
                var count = pointers.Count;
                for (var i = 0; i < count; i++)
                {
                    if (pointers[i] == activePointer) return true;
                }
                return false;
            }
        }

        /// <inheritdoc />
        protected override void reset()
        {
            base.reset();

            screenPixelRotationBuffer = 0f;
            angleBuffer = 0;
            screenPixelScalingBuffer = 0f;
            scaleBuffer = 1f;
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Calculates rotation.
        /// </summary>
        /// <param name="center"> Center screen position. </param>
        /// <param name="oldScreenPos"> Pointer old screen position. </param>
        /// <param name="newScreenPos"> Pointer new screen position. </param>
        /// <returns> Angle in degrees. </returns>
        protected virtual float doRotation(Vector3 center, Vector2 oldScreenPos, Vector2 newScreenPos, Camera camera)
        {
            return 0;
        }

        /// <summary>
        /// Calculates scaling.
        /// </summary>
        /// <param name="center"> Center screen position. </param>
        /// <param name="oldScreenPos"> Pointer old screen position. </param>
        /// <param name="newScreenPos"> Pointer new screen position. </param>
        /// <returns> Multiplicative delta scaling. </returns>
        protected virtual float doScaling(Vector3 center, Vector2 oldScreenPos, Vector2 newScreenPos, Camera camera)
        {
            return 1;
        }

        /// <summary>
        /// Returns previous screen position of a point with index 0.
        /// </summary>
        protected Vector2 getPointPreviousScreenPosition()
        {
            return activePointers[0].PreviousPosition;
        }

        #endregion

#if UNITY_EDITOR
        void ISelfValidator.Validate(SelfValidationResult result)
        {
            if ((type & TransformGesture.TransformType.Translation) != default)
            {
                result.AddError("OnePointTransformGestureBase should not have Translation type.");
            }
        }
#endif
    }
}