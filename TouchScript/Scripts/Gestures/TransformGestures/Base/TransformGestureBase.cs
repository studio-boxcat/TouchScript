/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Collections.Generic;
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

        public event Action TransformStarted;
        public event Action Transformed;
        public event Action TransformCompleted;

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets types of transformation this gesture supports.
        /// </summary>
        /// <value> Type flags. </value>
        public TransformGesture.TransformType Type => type;

        /// <inheritdoc />
        public TransformGesture.TransformType TransformMask => transformMask;

        /// <inheritdoc />
        public Vector3 DeltaPosition => deltaPosition;

        /// <inheritdoc />
        public float DeltaRotation => deltaRotation;

        /// <inheritdoc />
        public float DeltaScale => deltaScale;

        /// <inheritdoc />
        public Vector3 RotationAxis => rotationAxis;

        #endregion

        #region Private variables

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

        #region Gesture callbacks

        /// <inheritdoc />
        protected override void pointersPressed(IList<Pointer> pointers)
        {
            base.pointersPressed(pointers);

            if (pointersNumState == PointersNumState.None)
            {
                setState(GestureState.Possible);
            }
        }

        /// <inheritdoc />
        protected override void pointersReleased(IList<Pointer> pointers)
        {
            base.pointersReleased(pointers);

            if (pointersNumState == PointersNumState.None)
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
            TransformStarted?.InvokeHandleExceptions();
        }

        /// <inheritdoc />
        protected override void onChanged()
        {
            base.onChanged();

            targetPositionOverridden = false;

            Transformed?.InvokeHandleExceptions();
        }

        /// <inheritdoc />
        protected override void onRecognized()
        {
            base.onRecognized();

            TransformCompleted?.InvokeHandleExceptions();
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
    }
}