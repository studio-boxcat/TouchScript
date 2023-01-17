/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Collections.Generic;
using TouchScript.Devices.Display;
using TouchScript.Utils;
using TouchScript.Pointers;
using UnityEngine;

namespace TouchScript.Gestures.TransformGestures.Base
{
    /// <summary>
    /// Abstract base class for Transform Gestures.
    /// </summary>
    /// <remarks>
    /// <para>Relationship with <see cref="Behaviors.Transformer"/> component requires that if current object position is not exactly the one acquired by transformation events from this gesture (i.e. when smoothing is applied current transform is lagging a bit behind target transform), the gesture has to know about this to calculate translation properly. This is where <see cref="OverrideTargetPosition"/> method comes into play. <see cref="Behaviors.Transformer"/> has to call it after every transform event.</para>
    /// </remarks>
    public abstract class TransformGestureBase : Gesture, ITransformGesture
    {
        #region Events

        public event EventHandler<EventArgs> TransformStarted;
        public event EventHandler<EventArgs> Transformed;
        public event EventHandler<EventArgs> TransformCompleted;

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets types of transformation this gesture supports.
        /// </summary>
        /// <value> Type flags. </value>
        public TransformGesture.TransformType Type
        {
            get { return type; }
            set
            {
                type = value;
                updateType();
            }
        }

        /// <summary>
        /// Gets or sets minimum distance in cm for pointers to move for gesture to begin. 
        /// </summary>
        /// <value> Minimum value in cm user must move their fingers to start this gesture. </value>
        public float ScreenTransformThreshold
        {
            get { return screenTransformThreshold; }
            set
            {
                screenTransformThreshold = value;
                updateScreenTransformThreshold();
            }
        }

        /// <inheritdoc />
        public TransformGesture.TransformType TransformMask
        {
            get { return transformMask; }
        }

        /// <inheritdoc />
        public Vector3 DeltaPosition
        {
            get { return deltaPosition; }
        }

        /// <inheritdoc />
        public float DeltaRotation
        {
            get { return deltaRotation; }
        }

        /// <inheritdoc />
        public float DeltaScale
        {
            get { return deltaScale; }
        }

        /// <inheritdoc />
        public Vector3 RotationAxis
        {
            get { return rotationAxis; }
        }

        #endregion

        #region Private variables

        /// <summary>
        /// <see cref="ScreenTransformThreshold"/> in pixels.
        /// </summary>
        protected float screenTransformPixelThreshold;

        /// <summary>
        /// <see cref="ScreenTransformThreshold"/> in pixels squared.
        /// </summary>
        protected float screenTransformPixelThresholdSquared;

        /// <summary>
        /// The bit mask of what transform operations happened this frame.
        /// </summary>
        protected TransformGesture.TransformType transformMask;

        /// <summary>
        /// Calculated delta position.
        /// </summary>
        protected Vector3 deltaPosition;

        /// <summary>
        /// Calculated delta rotation.
        /// </summary>
        protected float deltaRotation;

        /// <summary>
        /// Calculated delta scale.
        /// </summary>
        protected float deltaScale;

        /// <summary>
        /// Rotation axis to use with deltaRotation.
        /// </summary>
        protected Vector3 rotationAxis = new Vector3(0, 0, 1);

        /// <summary>
        /// Indicates whether transformation started;
        /// </summary>
        protected bool isTransforming = false;

        /// <summary>
        /// Indicates if current position is being overridden for the next frame. <see cref="OverrideTargetPosition"/>.
        /// </summary>
        protected bool targetPositionOverridden = false;


        /// <summary>
        /// Target overridden position. <see cref="OverrideTargetPosition"/>.
        /// </summary>
        protected Vector3 targetPosition;

        /// <summary>
        /// The type of the transforms this gesture can dispatch.
        /// </summary>
        [SerializeField]
        protected TransformGesture.TransformType type = TransformGesture.TransformType.Translation | TransformGesture.TransformType.Scaling |
                                                        TransformGesture.TransformType.Rotation;

        [SerializeField]
        private float screenTransformThreshold = 0.1f;

        #endregion

        #region Public methods

        /// <summary>
        /// Overrides the target position used in calculations this frame. If used, has to be set after every transform event. <see cref="TransformGestureBase"/>.
        /// </summary>
        /// <param name="position">Target position.</param>
        public void OverrideTargetPosition(Vector3 position)
        {
            targetPositionOverridden = true;
            targetPosition = position;
        }

        #endregion

        #region Unity methods

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();
            updateScreenTransformThreshold();
            updateType();
        }

        #endregion

        #region Gesture callbacks

        /// <inheritdoc />
        protected override void pointersPressed(IList<Pointer> pointers)
        {
            base.pointersPressed(pointers);

            if (pointersNumState == PointersNumState.PassedMaxThreshold ||
                pointersNumState == PointersNumState.PassedMinMaxThreshold)
            {
                switch (State)
                {
                    case GestureState.Began:
                    case GestureState.Changed:
                        setState(GestureState.Ended);
                        break;
                }
            } else if (pointersNumState == PointersNumState.PassedMinThreshold)
            {
                setState(GestureState.Possible);
            }
        }

        /// <inheritdoc />
        protected override void pointersReleased(IList<Pointer> pointers)
        {
            base.pointersReleased(pointers);

            if (pointersNumState == PointersNumState.PassedMinThreshold)
            {
                switch (State)
                {
                    case GestureState.Began:
                    case GestureState.Changed:
                        setState(GestureState.Ended);
                        break;
                    case GestureState.Possible:
                        setState(GestureState.Idle);
                        break;
                }
            }
        }

        /// <inheritdoc />
        protected override void onBegan()
        {
            base.onBegan();
            TransformStarted?.InvokeHandleExceptions(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        protected override void onChanged()
        {
            base.onChanged();

            targetPositionOverridden = false;

            Transformed?.InvokeHandleExceptions(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        protected override void onRecognized()
        {
            base.onRecognized();

            TransformCompleted?.InvokeHandleExceptions(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        protected override void reset()
        {
            base.reset();

            resetValues();
            isTransforming = false;
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Updates the type of the gesture.
        /// </summary>
        protected virtual void updateType() {}

        /// <summary>
        /// Resets the frame delta values.
        /// </summary>
        protected void resetValues()
		{
			deltaPosition = Vector3.zero;
			deltaRotation = 0f;
			deltaScale = 1f;
			transformMask = 0;
		}

        #endregion

        #region Private functions

        private void updateScreenTransformThreshold()
        {
            screenTransformPixelThreshold = screenTransformThreshold * DisplayDevice.DotsPerCentimeter;
            screenTransformPixelThresholdSquared = screenTransformPixelThreshold * screenTransformPixelThreshold;
        }

        #endregion
    }
}