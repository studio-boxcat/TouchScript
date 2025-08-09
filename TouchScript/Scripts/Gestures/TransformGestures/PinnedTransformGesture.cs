/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
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
        #region Private variables

        [SerializeField]
        private ProjectionType projection = ProjectionType.Layer;

        [NonSerialized]
        private TouchLayer projectionLayer;

        private Plane transformPlane;

        #endregion

        #region Gesture callbacks

        /// <inheritdoc />
        protected override void pointersPressed(IList<Pointer> pointers)
        {
            base.pointersPressed(pointers);

            if (activePointers.Count == pointers.Count)
            {
                projectionLayer = activePointers[0].GetPressData().Layer;
                var normal = projection.GetNormal(projectionLayer, cachedTransform);
                transformPlane = new Plane(normal, cachedTransform.position);
                rotationAxis = transformPlane.normal;
            }
        }

        #endregion

        #region Protected methods

        /// <inheritdoc />
        protected override float doRotation(Vector3 center, Vector2 oldScreenPos, Vector2 newScreenPos, Camera camera)
        {
            if (projectionLayer is null)
            {
                L.W("[PinnedTransformGesture] Can't rotate: no projection layer.", this);
                return 0;
            }

            var newVector = camera.ProjectTo(newScreenPos, transformPlane) - center;
            var oldVector = camera.ProjectTo(oldScreenPos, transformPlane) - center;
            var angle = Vector3.Angle(oldVector, newVector);
            if (Vector3.Dot(Vector3.Cross(oldVector, newVector), transformPlane.normal) < 0)
                angle = -angle;
            return angle;
        }

        /// <inheritdoc />
        protected override float doScaling(Vector3 center, Vector2 oldScreenPos, Vector2 newScreenPos, Camera camera)
        {
            if (projectionLayer is null)
            {
                L.W("[PinnedTransformGesture] Can't scale: no projection layer.", this);
                return 1;
            }

            var newVector = camera.ProjectTo(newScreenPos, transformPlane) - center;
            var oldVector = camera.ProjectTo(oldScreenPos, transformPlane) - center;
            return newVector.magnitude / oldVector.magnitude;
        }

        #endregion
    }
}