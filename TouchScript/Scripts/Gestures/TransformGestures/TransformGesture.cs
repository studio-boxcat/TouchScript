/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Collections.Generic;
using TouchScript.Gestures.TransformGestures.Base;
using TouchScript.Layers;
using TouchScript.Utils;
using TouchScript.Pointers;
using UnityEngine;

namespace TouchScript.Gestures.TransformGestures
{
    /// <summary>
    /// Recognizes a transform gesture, i.e. translation, rotation, scaling or a combination of these.
    /// </summary>
    [AddComponentMenu("TouchScript/Gestures/Transform Gesture")]
    [HelpURL("http://touchscript.github.io/docs/html/T_TouchScript_Gestures_TransformGestures_TransformGesture.htm")]
    public class TransformGesture : TwoPointTransformGestureBase
    {
        #region Constants

        /// <summary>
        /// Types of transformation.
        /// </summary>
        [Flags]
        public enum TransformType
        {
            /// <summary>
            /// No transform.
            /// </summary>
            None = 0,

            /// <summary>
            /// Translation.
            /// </summary>
            Translation = 0x1,

            /// <summary>
            /// Rotation.
            /// </summary>
            Rotation = 0x2,

            /// <summary>
            /// Scaling.
            /// </summary>
            Scaling = 0x4
        }

        /// <summary>
        /// Transform's projection type.
        /// </summary>
        public enum ProjectionType
        {
            /// <summary>
            /// Use a plane with normal vector defined by layer.
            /// </summary>
            Layer,

            /// <summary>
            /// Use a plane with certain normal vector in local coordinates.
            /// </summary>
            Object,

            /// <summary>
            /// Use a plane with certain normal vector in global coordinates.
            /// </summary>
            Global,
        }

        #endregion

        #region Private variables

        [SerializeField]
        private ProjectionType projection = ProjectionType.Layer;

        [SerializeField]
        private Vector3 projectionPlaneNormal = Vector3.forward;

        private TouchLayer projectionLayer;
        private Plane transformPlane;

        #endregion

        #region Unity methods

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            updateProjectionPlane();
        }

        #endregion

        #region Gesture callbacks

        /// <inheritdoc />
        protected override void pointersPressed(IList<Pointer> pointers)
        {
            base.pointersPressed(pointers);

            if (activePointers.Count == pointers.Count)
            {
                projectionLayer = activePointers[0].GetPressData().Layer;
                updateProjectionPlane();
            }
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Projects the point which was scaled and rotated.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="dR">Delta rotation.</param>
        /// <param name="dS">Delta scale.</param>
        /// <returns></returns>
        protected Vector3 projectScaledRotated(Vector2 point, float dR, float dS, Camera camera)
        {
            var center = targetPositionOverridden ? targetPosition : cachedTransform.position;
            var delta = camera.ProjectTo(point, transformPlane) - center;
            if (dR != 0) delta = Quaternion.AngleAxis(dR, RotationAxis) * delta;
            if (dS != 0) delta = delta * dS;
            return center + delta;
        }

        /// <inheritdoc />
        protected override float doRotation(Vector2 oldScreenPos1, Vector2 oldScreenPos2, Vector2 newScreenPos1,
                                            Vector2 newScreenPos2, Camera camera)
        {
            var newVector = camera.ProjectTo(newScreenPos2, transformPlane) -
                            camera.ProjectTo(newScreenPos1, transformPlane);
            var oldVector = camera.ProjectTo(oldScreenPos2, transformPlane) -
                            camera.ProjectTo(oldScreenPos1, transformPlane);
            var angle = Vector3.Angle(oldVector, newVector);
            if (Vector3.Dot(Vector3.Cross(oldVector, newVector), transformPlane.normal) < 0)
                angle = -angle;
            return angle;
        }

        /// <inheritdoc />
        protected override float doScaling(Vector2 oldScreenPos1, Vector2 oldScreenPos2, Vector2 newScreenPos1, Vector2 newScreenPos2, Camera camera)
        {
            var newVector = camera.ProjectTo(newScreenPos2, transformPlane) -
                            camera.ProjectTo(newScreenPos1, transformPlane);
            var oldVector = camera.ProjectTo(oldScreenPos2, transformPlane) -
                            camera.ProjectTo(oldScreenPos1, transformPlane);
            return newVector.magnitude / oldVector.magnitude;
        }

        /// <inheritdoc />
        protected override Vector3 doOnePointTranslation(Vector2 oldScreenPos, Vector2 newScreenPos, Camera camera)
        {
            if (isTransforming)
            {
                return camera.ProjectTo(newScreenPos, transformPlane) -
                       camera.ProjectTo(oldScreenPos, transformPlane);
            }

            screenPixelTranslationBuffer += newScreenPos - oldScreenPos;
            if (screenPixelTranslationBuffer.sqrMagnitude > screenTransformPixelThresholdSquared)
            {
                isTransforming = true;
                return camera.ProjectTo(newScreenPos, transformPlane) -
                       camera.ProjectTo(newScreenPos - screenPixelTranslationBuffer, transformPlane);
            }

            return Vector3.zero;
        }

        /// <inheritdoc />
        protected override Vector3 doTwoPointTranslation(Vector2 oldScreenPos1, Vector2 oldScreenPos2,
                                                         Vector2 newScreenPos1, Vector2 newScreenPos2, float dR, float dS, Camera camera)
        {
            if (isTransforming)
            {
                return camera.ProjectTo(newScreenPos1, transformPlane) - projectScaledRotated(oldScreenPos1, dR, dS, camera);
            }

            screenPixelTranslationBuffer += newScreenPos1 - oldScreenPos1;
            if (screenPixelTranslationBuffer.sqrMagnitude > screenTransformPixelThresholdSquared)
            {
                isTransforming = true;
                return camera.ProjectTo(newScreenPos1, transformPlane) -
                       projectScaledRotated(newScreenPos1 - screenPixelTranslationBuffer, dR, dS, camera);
            }

            return Vector3.zero;
        }

        #endregion

        #region Private functions

        /// <summary>
        /// Updates projection plane based on options set.
        /// </summary>
        private void updateProjectionPlane()
        {
            if (!Application.isPlaying) return;

            var normal = projection switch
            {
                ProjectionType.Layer => projectionLayer == null
                    ? cachedTransform.TransformDirection(Vector3.forward)
                    : projectionLayer.transform.forward,
                ProjectionType.Object => cachedTransform.TransformDirection(projectionPlaneNormal),
                ProjectionType.Global => projectionPlaneNormal,
                _ => throw new ArgumentOutOfRangeException()
            };
            transformPlane = new Plane(normal, cachedTransform.position);

            rotationAxis = transformPlane.normal;
        }

        #endregion
    }
}