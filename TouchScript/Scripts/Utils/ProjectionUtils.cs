using System;
using JetBrains.Annotations;
using TouchScript.Layers;
using UnityEngine;

namespace TouchScript.Utils
{
    /// <summary>
    /// Transform's projection type.
    /// </summary>
    enum ProjectionType : byte
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

    static class ProjectionUtils
    {
        public static Vector3 ProjectTo(this Camera camera, Vector2 screenPosition, Plane projectionPlane)
        {
            var ray = camera.ScreenPointToRay(screenPosition);
            var result = projectionPlane.Raycast(ray, out var distance);
            if (!result && Mathf.Approximately(distance, 0f))
                return -projectionPlane.normal * projectionPlane.GetDistanceToPoint(default); // perpendicular to the screen

            return ray.origin + ray.direction * distance;
        }

        [MustUseReturnValue]
        public static Vector3 GetNormal(this ProjectionType projection, TouchLayer projectionLayer, Transform objectTransform)
        {
            return projection switch
            {
                ProjectionType.Layer => projectionLayer.transform.forward,
                ProjectionType.Object => objectTransform.TransformDirection(Vector3.forward),
                ProjectionType.Global => Vector3.forward,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}