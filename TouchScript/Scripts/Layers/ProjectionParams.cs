/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using TouchScript.Utils.Geom;
using UnityEngine;

namespace TouchScript.Layers
{
    /// <summary>
    /// Projection parameters for a camera based <see cref="TouchLayer"/>.
    /// </summary>
    public readonly struct ProjectionParams
    {
        /// <summary>
        /// Camera used for projection.
        /// </summary>
        readonly Camera _camera;

        public ProjectionParams(Camera camera)
        {
            _camera = camera;
        }

        public Vector3 ProjectTo(Vector2 screenPosition, Plane projectionPlane)
        {
            return ProjectionUtils.CameraToPlaneProjection(screenPosition, _camera, projectionPlane);
        }

        public Vector2 ProjectFrom(Vector3 worldPosition)
        {
            return _camera.WorldToScreenPoint(worldPosition);
        }
    }
}