/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using TouchScript.Gestures.TransformGestures.Base;
using TouchScript.Layers;
using TouchScript.Pointers;
using UnityEngine;

namespace TouchScript.Gestures.TransformGestures
{
    /// <summary>
    /// Recognizes a transform gesture around center of the object, i.e. one finger rotation, scaling or a combination of these.
    /// </summary>
    [AddComponentMenu("TouchScript/Gestures/Pinned Transform Gesture")]
    [HelpURL("http://touchscript.github.io/docs/html/T_TouchScript_Gestures_TransformGestures_PinnedTransformGesture.htm")]
    public class PinnedTransformGesture : OnePointTransformGestureBase
    {
        #region Public properties

        /// <summary>
        /// Plane where transformation occured.
        /// </summary>
        public Plane TransformPlane => transformPlane;

        #endregion

        #region Private variables

        [SerializeField]
        private TransformGesture.ProjectionType projection = TransformGesture.ProjectionType.Layer;

        [SerializeField]
        private Vector3 projectionPlaneNormal = Vector3.forward;

        private TouchLayer projectionLayer;
        private Plane transformPlane;

        #endregion

        #region Public methods

        #endregion

        #region Unity methods

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();

            transformPlane = new Plane();
        }

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

            if (NumPointers == pointers.Count)
            {
                projectionLayer = activePointers[0].GetPressData().Layer;
                updateProjectionPlane();
            }
        }

        #endregion

        #region Protected methods

        /// <inheritdoc />
        protected override float doRotation(Vector3 center, Vector2 oldScreenPos, Vector2 newScreenPos,
                                 ProjectionParams projectionParams)
        {
            var newVector = projectionParams.ProjectTo(newScreenPos, TransformPlane) - center;
            var oldVector = projectionParams.ProjectTo(oldScreenPos, TransformPlane) - center;
            var angle = Vector3.Angle(oldVector, newVector);
            if (Vector3.Dot(Vector3.Cross(oldVector, newVector), TransformPlane.normal) < 0)
                angle = -angle;
            return angle;
        }

        /// <inheritdoc />
        protected override float doScaling(Vector3 center, Vector2 oldScreenPos, Vector2 newScreenPos,
                                ProjectionParams projectionParams)
        {
            var newVector = projectionParams.ProjectTo(newScreenPos, TransformPlane) - center;
            var oldVector = projectionParams.ProjectTo(oldScreenPos, TransformPlane) - center;
            return newVector.magnitude / oldVector.magnitude;
        }

        #endregion

        #region Private functions

        private void updateProjectionPlane()
        {
            switch (projection)
            {
                case TransformGesture.ProjectionType.Layer:
                    if (projectionLayer == null)
                        transformPlane = new Plane(cachedTransform.TransformDirection(Vector3.forward),
                            cachedTransform.position);
                    else transformPlane = new Plane(projectionLayer.transform.forward, cachedTransform.position);
                    break;
                case TransformGesture.ProjectionType.Object:
                    transformPlane = new Plane(cachedTransform.TransformDirection(projectionPlaneNormal),
                        cachedTransform.position);
                    break;
                case TransformGesture.ProjectionType.Global:
                    transformPlane = new Plane(projectionPlaneNormal, cachedTransform.position);
                    break;
            }

            rotationAxis = transformPlane.normal;
        }

        #endregion
    }
}