using UnityEngine;

namespace TouchScript.Utils
{
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
    }
}