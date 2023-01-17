/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using TouchScript.Layers;
using TouchScript.Pointers;
using TouchScript.Utils;
using TouchScript.Utils.Geom;
using UnityEngine;

namespace TouchScript.Gestures.TransformGestures.Base
{
    /// <summary>
    /// Abstract base class for Pinned Transform Gestures.
    /// </summary>
    public abstract class OnePointTransformGestureBase : TransformGestureBase
    {
        #region Constants

        #endregion

        #region Events

        #endregion

        #region Public properties

        /// <inheritdoc />
        public override Vector2 ScreenPosition
        {
            get
            {
                if (NumPointers == 0) return InvalidPosition.Value;
                return activePointers[0].Position;
            }
        }

        /// <inheritdoc />
        public override Vector2 PreviousScreenPosition
        {
            get
            {
                if (NumPointers == 0) return InvalidPosition.Value;
                return activePointers[0].PreviousPosition;
            }
        }

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
            var projectionParams = thePointer.GetPressData().Layer.GetProjectionParams();
            var dR = deltaRotation = 0;
            var dS = deltaScale = 1f;

            if (pointersNumState != PointersNumState.InRange) return;

            var rotationEnabled = (Type & TransformGesture.TransformType.Rotation) == TransformGesture.TransformType.Rotation;
            var scalingEnabled = (Type & TransformGesture.TransformType.Scaling) == TransformGesture.TransformType.Scaling;
            if (!rotationEnabled && !scalingEnabled) return;
            if (!relevantPointers(pointers)) return;

            var worldCenter = cachedTransform.position;
            var screenCenter = projectionParams.ProjectFrom(worldCenter);
            var newScreenPos = thePointer.Position;

            // Here we can't reuse last frame screen positions because points 0 and 1 can change.
            // For example if the first of 3 fingers is lifted off.
            var oldScreenPos = getPointPreviousScreenPosition();

            if (rotationEnabled)
            {
                if (isTransforming)
                {
                    dR = doRotation(worldCenter, oldScreenPos, newScreenPos, projectionParams);
                }
                else
                {
                    // Find how much we moved perpendicular to the line (center, oldScreenPos)
                    screenPixelRotationBuffer += TwoD.PointToLineDistance(screenCenter, oldScreenPos, newScreenPos);
                    angleBuffer += doRotation(worldCenter, oldScreenPos, newScreenPos, projectionParams);

                    if (screenPixelRotationBuffer * screenPixelRotationBuffer >=
                        screenTransformPixelThresholdSquared)
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
                    dS *= doScaling(worldCenter, oldScreenPos, newScreenPos, projectionParams);
                }
                else
                {
                    screenPixelScalingBuffer += (newScreenPos - screenCenter).magnitude -
                                                (oldScreenPos - screenCenter).magnitude;
                    scaleBuffer *= doScaling(worldCenter, oldScreenPos, newScreenPos, projectionParams);

                    if (screenPixelScalingBuffer * screenPixelScalingBuffer >=
                        screenTransformPixelThresholdSquared)
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
        /// <param name="projectionParams"> Layer projection parameters. </param>
        /// <returns> Angle in degrees. </returns>
        protected virtual float doRotation(Vector3 center, Vector2 oldScreenPos, Vector2 newScreenPos,
            ProjectionParams projectionParams)
        {
            return 0;
        }

        /// <summary>
        /// Calculates scaling.
        /// </summary>
        /// <param name="center"> Center screen position. </param>
        /// <param name="oldScreenPos"> Pointer old screen position. </param>
        /// <param name="newScreenPos"> Pointer new screen position. </param>
        /// <param name="projectionParams"> Layer projection parameters. </param>
        /// <returns> Multiplicative delta scaling. </returns>
        protected virtual float doScaling(Vector3 center, Vector2 oldScreenPos, Vector2 newScreenPos,
            ProjectionParams projectionParams)
        {
            return 1;
        }

        /// <summary>
        /// Checks if there are pointers in the list which matter for the gesture.
        /// </summary>
        /// <param name="pointers"> List of pointers </param>
        /// <returns> <c>true</c> if there are relevant pointers; <c>false</c> otherwise.</returns>
        protected virtual bool relevantPointers(IList<Pointer> pointers)
        {
            // We care only about the first pointer
            var count = pointers.Count;
            for (var i = 0; i < count; i++)
            {
                if (pointers[i] == activePointers[0]) return true;
            }
            return false;
        }

        /// <summary>
        /// Returns previous screen position of a point with index 0.
        /// </summary>
        protected virtual Vector2 getPointPreviousScreenPosition()
        {
            return activePointers[0].PreviousPosition;
        }

        /// <inheritdoc />
        protected override void updateType()
        {
            type = type & ~TransformGesture.TransformType.Translation;
        }

        #endregion

        #region Private functions

        #endregion
    }
}