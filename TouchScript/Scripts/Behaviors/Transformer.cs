/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using TouchScript.Core;
using TouchScript.Gestures;
using TouchScript.Gestures.TransformGestures;
using TouchScript.Gestures.TransformGestures.Base;
using UnityEngine;

namespace TouchScript.Behaviors
{
    /// <summary>
    /// Component which transforms an object according to events from transform gestures: <see cref="TransformGesture"/>, <see cref="ScreenTransformGesture"/>, <see cref="PinnedTransformGesture"/> and others.
    /// </summary>
    [AddComponentMenu("TouchScript/Behaviors/Transformer")]
    [HelpURL("http://touchscript.github.io/docs/html/T_TouchScript_Behaviors_Transformer.htm")]
    public class Transformer : MonoBehaviour
    {
        // Here's how it works.
        //
        // If smoothing is not enabled, the component just gets gesture events in stateChangedHandler(), passes Changed event to manualUpdate() which calls applyValues() to sett updated values.
        // The value of transformMask is used to only set values which were changed not to interfere with scripts changing this values.
        //
        // If smoothing is enabled — targetPosition, targetScale, targetRotation are cached and a lerp from current position to these target positions is applied every frame in update() method. It also checks transformMask to change only needed values.
        // If none of the delta values pass the threshold, the component transitions to idle state.

        #region Consts

        /// <summary>
        /// State for internal Transformer state machine.
        /// </summary>
        private enum TransformerState
        {
            /// <summary>
            /// Nothing is happening.
            /// </summary>
            Idle,

            /// <summary>
            /// The object is under manual control, i.e. user is transforming it.
            /// </summary>
            Manual,

            /// <summary>
            /// The object is under automatic control, i.e. it's being smoothly moved into target position when user lifted all fingers off.
            /// </summary>
            Automatic
        }

        #endregion

        #region Private variables

        [SerializeField]
        private bool enableSmoothing = false;

        [SerializeField]
        private float smoothingFactor = 1f / 100000f;

        [SerializeField]
        private float positionThreshold = 0.01f;

        [SerializeField]
        private float rotationThreshold = 0.1f;

        [SerializeField]
        private float scaleThreshold = 0.01f;

        [SerializeField]
        private bool allowChangingFromOutside = false;

        private TransformerState state;

        private TransformGestureBase gesture;
        private Transform cachedTransform;

        private TransformGesture.TransformType transformMask;
        private Vector3 targetPosition, targetScale;
        private Quaternion targetRotation;

        // last* variables are needed to detect when Transform's properties were changed outside of this script
        private Vector3 lastPosition, lastScale;
        private Quaternion lastRotation;

        #endregion

        #region Unity methods

        private void Awake()
        {
            cachedTransform = transform;
        }

        private void OnEnable()
        {
            gesture = GetComponent<TransformGestureBase>();
            gesture.StateChanged += stateChangedHandler;
            var touchManager = TouchManager.Instance;
            if (touchManager != null) touchManager.FrameFinished += frameFinishedHandler;

            stateIdle();
        }

        private void OnDisable()
        {
            if (gesture != null) gesture.StateChanged -= stateChangedHandler;
            var touchManager = TouchManager.Instance;
            if (touchManager != null) touchManager.FrameFinished -= frameFinishedHandler;

            stateIdle();
        }

        #endregion

        #region States

        private void stateIdle()
        {
            var prevState = state;
            setState(TransformerState.Idle);

            if (enableSmoothing && prevState == TransformerState.Automatic)
            {
                transform.position = lastPosition = targetPosition;
                var newLocalScale = lastScale = targetScale;
                // prevent recalculating colliders when no scale occurs
                if (newLocalScale != transform.localScale) transform.localScale = newLocalScale;
                transform.rotation = lastRotation = targetRotation;
            }

            transformMask = TransformGesture.TransformType.None;
        }

        private void stateManual()
        {
            setState(TransformerState.Manual);

            targetPosition = lastPosition = cachedTransform.position;
            targetRotation = lastRotation = cachedTransform.rotation;
            targetScale = lastScale = cachedTransform.localScale;
            transformMask = TransformGesture.TransformType.None;
        }

        private void stateAutomatic()
        {
            setState(TransformerState.Automatic);

            if (!enableSmoothing || transformMask == TransformGesture.TransformType.None) stateIdle();
        }

        private void setState(TransformerState newState)
        {
            state = newState;
        }

        #endregion

        #region Private functions

        private void update()
        {
            if (state == TransformerState.Idle) return;

            if (!enableSmoothing) return;

            var fraction = 1 - Mathf.Pow(smoothingFactor, Time.unscaledDeltaTime);
            var changed = false;

            if ((transformMask & TransformGesture.TransformType.Scaling) != 0)
            {
                var scale = transform.localScale;
                if (allowChangingFromOutside)
                {
                    // Changed by someone else.
                    // Need to make sure to check per component here.
                    if (!Mathf.Approximately(scale.x, lastScale.x))
                        targetScale.x = scale.x;
                    if (!Mathf.Approximately(scale.y, lastScale.y))
                        targetScale.y = scale.y;
                    if (!Mathf.Approximately(scale.z, lastScale.z))
                        targetScale.z = scale.z;
                }
                var newLocalScale = Vector3.Lerp(scale, targetScale, fraction);
                // Prevent recalculating colliders when no scale occurs.
                if (newLocalScale != scale)
                {
                    transform.localScale = newLocalScale;
                    // Something might have adjusted our scale.
                    lastScale = transform.localScale;
                }

                if (state == TransformerState.Automatic && !changed && (targetScale - lastScale).sqrMagnitude > scaleThreshold) changed = true;
            }

            if ((transformMask & TransformGesture.TransformType.Rotation) != 0)
            {
                if (allowChangingFromOutside)
                {
                    // Changed by someone else.
                    if (transform.rotation != lastRotation) targetRotation = transform.rotation;
                }
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, fraction);
                // Something might have adjusted our rotation.
                lastRotation = transform.rotation;

                if (state == TransformerState.Automatic && !changed && Quaternion.Angle(targetRotation, lastRotation) > rotationThreshold) changed = true;
            }

            if ((transformMask & TransformGesture.TransformType.Translation) != 0)
            {
                var pos = transform.position;
                if (allowChangingFromOutside)
                {
                    // Changed by someone else.
                    // Need to make sure to check per component here.
                    if (!Mathf.Approximately(pos.x, lastPosition.x))
                        targetPosition.x = pos.x;
                    if (!Mathf.Approximately(pos.y, lastPosition.y))
                        targetPosition.y = pos.y;
                    if (!Mathf.Approximately(pos.z, lastPosition.z))
                        targetPosition.z = pos.z;
                }
                transform.position = Vector3.Lerp(pos, targetPosition, fraction);
                // Something might have adjusted our position (most likely Unity UI).
                lastPosition = transform.position;

                if (state == TransformerState.Automatic && !changed && (targetPosition - lastPosition).sqrMagnitude > positionThreshold) changed = true;
            }

            if (state == TransformerState.Automatic && !changed) stateIdle();
        }

        private void manualUpdate()
        {
            if (state != TransformerState.Manual) stateManual();

            var mask = gesture.TransformMask;
            if ((mask & TransformGesture.TransformType.Scaling) != 0) targetScale *= gesture.DeltaScale;
            if ((mask & TransformGesture.TransformType.Rotation) != 0)
                targetRotation = Quaternion.AngleAxis(gesture.DeltaRotation, gesture.RotationAxis) * targetRotation;
            if ((mask & TransformGesture.TransformType.Translation) != 0) targetPosition += gesture.DeltaPosition;
            transformMask |= mask;

            gesture.OverrideTargetPosition(targetPosition);

            if (!enableSmoothing) applyValues();
        }

        private void applyValues()
        {
            if ((transformMask & TransformGesture.TransformType.Scaling) != 0) cachedTransform.localScale = targetScale;
            if ((transformMask & TransformGesture.TransformType.Rotation) != 0) cachedTransform.rotation = targetRotation;
            if ((transformMask & TransformGesture.TransformType.Translation) != 0) cachedTransform.position = targetPosition;
            transformMask = TransformGesture.TransformType.None;
        }

        #endregion

        #region Event handlers

        private void stateChangedHandler(GestureState oldState, GestureState newState)
        {
            switch (newState)
            {
                case GestureState.Possible:
                    stateManual();
                    break;
                case GestureState.Changed:
                    manualUpdate();
                    break;
                case GestureState.Ended:
                case GestureState.Cancelled:
                    stateAutomatic();
                    break;
                case GestureState.Failed:
                case GestureState.Idle:
                    if (oldState == GestureState.Possible) stateAutomatic();
                    break;
            }
        }

        private void frameFinishedHandler()
        {
            update();
        }

        #endregion
    }
}