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
        readonly Camera camera;

        public ProjectionParams(Camera camera)
        {
            this.camera = camera;
        }

        public Vector3 ProjectTo(Vector2 screenPosition, Plane projectionPlane)
        {
            return ProjectionUtils.CameraToPlaneProjection(screenPosition, camera, projectionPlane);
        }

        public Vector2 ProjectFrom(Vector3 worldPosition)
        {
            return camera.WorldToScreenPoint(worldPosition);
        }
    }
}