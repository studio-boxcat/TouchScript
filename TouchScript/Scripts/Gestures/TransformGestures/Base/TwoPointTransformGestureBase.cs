/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using TouchScript.Pointers;
using UnityEngine;

namespace TouchScript.Gestures.TransformGestures.Base
{
    /// <summary>
    /// Abstract base classfor two-point transform gestures.
    /// </summary>
    public abstract class TwoPointTransformGestureBase : TransformGestureBase
    {
        #region Private variables

        /// <summary>
        /// Translation buffer.
        /// </summary>
        protected Vector2 screenPixelTranslationBuffer;

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
            var dP = deltaPosition = Vector3.zero;
            var dR = deltaRotation = 0;
            var dS = deltaScale = 1f;

            if (pointersNumState != PointersNumState.Exists) return;

            var translationEnabled = (Type & TransformGesture.TransformType.Translation) == TransformGesture.TransformType.Translation;
            var rotationEnabled = (Type & TransformGesture.TransformType.Rotation) == TransformGesture.TransformType.Rotation;
            var scalingEnabled = (Type & TransformGesture.TransformType.Scaling) == TransformGesture.TransformType.Scaling;

            // one pointer or one cluster (points might be too close to each other for 2 clusters)
            if (activePointers.Count == 1 || (!rotationEnabled && !scalingEnabled))
            {
                if (!translationEnabled) return; // don't look for translates
                if (!relevantPointers1(pointers, thePointer)) return;

                // translate using one point
                dP = doOnePointTranslation(thePointer.PreviousPosition, thePointer.Position, targetCamera);
            }
            else
            {
                // Make sure that we actually care about the pointers moved.
                var thePointer2 = activePointers[1];
                if (!relevantPointers2(pointers, thePointer, thePointer2)) return;

                var newScreenPos1 = thePointer.Position;
                var newScreenPos2 = thePointer2.Position;

                // Here we can't reuse last frame screen positions because points 0 and 1 can change.
                // For example if the first of 3 fingers is lifted off.
                var oldScreenPos1 = thePointer.PreviousPosition;
                var oldScreenPos2 = thePointer2.PreviousPosition;

                var newScreenDelta = newScreenPos2 - newScreenPos1;
                if (DisplayDevice.CheckScreenPointsDistance(newScreenDelta))
                {
                    if (rotationEnabled)
                    {
                        if (isTransforming)
                        {
                            dR = doRotation(oldScreenPos1, oldScreenPos2, newScreenPos1, newScreenPos2, targetCamera);
                        }
                        else
                        {
                            float d1, d2;
                            // Find how much we moved perpendicular to the line (oldScreenPos1, oldScreenPos2)
                            TwoD.PointToLineDistance2(oldScreenPos1, oldScreenPos2, newScreenPos1, newScreenPos2,
                                out d1, out d2);
                            screenPixelRotationBuffer += (d1 - d2);
                            angleBuffer += doRotation(oldScreenPos1, oldScreenPos2, newScreenPos1, newScreenPos2, targetCamera);

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
                            dS *= doScaling(oldScreenPos1, oldScreenPos2, newScreenPos1, newScreenPos2, targetCamera);
                        }
                        else
                        {
                            var oldScreenDelta = oldScreenPos2 - oldScreenPos1;
                            var newDistance = newScreenDelta.magnitude;
                            var oldDistance = oldScreenDelta.magnitude;
                            screenPixelScalingBuffer += newDistance - oldDistance;
                            scaleBuffer *= doScaling(oldScreenPos1, oldScreenPos2, newScreenPos1, newScreenPos2, targetCamera);

                            if (DisplayDevice.CheckScreenTransformPixelThreshold(screenPixelScalingBuffer))
                            {
                                isTransforming = true;
                                dS = scaleBuffer;
                            }
                        }
                    }

                    if (translationEnabled)
                    {
                        if (dR == 0 && dS == 1) dP = doOnePointTranslation(oldScreenPos1, newScreenPos1, targetCamera);
                        else
                            dP = doTwoPointTranslation(oldScreenPos1, oldScreenPos2, newScreenPos1, newScreenPos2, dR, dS, targetCamera);
                    }
                }
                else if (translationEnabled)
                {
                    // points are too close, translate using one point
                    dP = doOnePointTranslation(oldScreenPos1, newScreenPos1, targetCamera);
                }
            }

            if (dP != Vector3.zero) transformMask |= TransformGesture.TransformType.Translation;
            if (dR != 0) transformMask |= TransformGesture.TransformType.Rotation;
            if (dS != 1) transformMask |= TransformGesture.TransformType.Scaling;

            if (transformMask != 0)
            {
                if (State == GestureState.Possible) setState(GestureState.Began);
                switch (State)
                {
                    case GestureState.Began:
                    case GestureState.Changed:
                        deltaPosition = dP;
                        deltaRotation = dR;
                        deltaScale = dS;
                        setState(GestureState.Changed);
                        resetValues();
                        break;
                }
            }
            return;


            static bool relevantPointers1(IList<Pointer> pointers, Pointer activePointer)
            {
                // We care only about the first pointer
                var count = pointers.Count;
                for (var i = 0; i < count; i++)
                {
                    if (pointers[i] == activePointer) return true;
                }
                return false;
            }

            static bool relevantPointers2(IList<Pointer> pointers, Pointer activePointer1, Pointer activePointer2)
            {
                // We care only about the first and the second pointers
                var count = pointers.Count;
                for (var i = 0; i < count; i++)
                {
                    var pointer = pointers[i];
                    if (pointer == activePointer1 || pointer == activePointer2) return true;
                }
                return false;
            }
        }

        /// <inheritdoc />
        protected override void reset()
        {
            base.reset();

            screenPixelTranslationBuffer = Vector2.zero;
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
        /// <param name="oldScreenPos1"> Finger one old screen position. </param>
        /// <param name="oldScreenPos2"> Finger two old screen position. </param>
        /// <param name="newScreenPos1"> Finger one new screen position. </param>
        /// <param name="newScreenPos2"> Finger two new screen position. </param>
        /// <returns> Angle in degrees. </returns>
        protected virtual float doRotation(Vector2 oldScreenPos1, Vector2 oldScreenPos2, Vector2 newScreenPos1,
            Vector2 newScreenPos2, Camera camera)
        {
            return 0;
        }

        /// <summary>
        /// Calculates scaling.
        /// </summary>
        /// <param name="oldScreenPos1"> Finger one old screen position. </param>
        /// <param name="oldScreenPos2"> Finger two old screen position. </param>
        /// <param name="newScreenPos1"> Finger one new screen position. </param>
        /// <param name="newScreenPos2"> Finger two new screen position. </param>
        /// <returns> Multiplicative delta scaling. </returns>
        protected virtual float doScaling(Vector2 oldScreenPos1, Vector2 oldScreenPos2, Vector2 newScreenPos1,
            Vector2 newScreenPos2, Camera camera)
        {
            return 1;
        }

        /// <summary>
        /// Calculates single finger translation.
        /// </summary>
        /// <param name="oldScreenPos"> Finger old screen position. </param>
        /// <param name="newScreenPos"> Finger new screen position. </param>
        /// <returns> Delta translation vector. </returns>
        protected virtual Vector3 doOnePointTranslation(Vector2 oldScreenPos, Vector2 newScreenPos, Camera camera)
        {
            return Vector3.zero;
        }

        /// <summary>
        /// Calculated two finger translation with respect to rotation and scaling.
        /// </summary>
        /// <param name="oldScreenPos1"> Finger one old screen position. </param>
        /// <param name="oldScreenPos2"> Finger two old screen position. </param>
        /// <param name="newScreenPos1"> Finger one new screen position. </param>
        /// <param name="newScreenPos2"> Finger two new screen position. </param>
        /// <param name="dR"> Calculated delta rotation. </param>
        /// <param name="dS"> Calculated delta scaling. </param>
        /// <returns> Delta translation vector. </returns>
        protected virtual Vector3 doTwoPointTranslation(Vector2 oldScreenPos1, Vector2 oldScreenPos2,
            Vector2 newScreenPos1, Vector2 newScreenPos2, float dR, float dS, Camera camera)
        {
            return Vector3.zero;
        }

        #endregion
    }
}