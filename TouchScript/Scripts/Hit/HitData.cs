using TouchScript.Layers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TouchScript.Hit
{
    /// <summary>
    /// An object representing a point hit by a pointer in 3D, 2D or UI space.
    /// </summary>
    public readonly struct HitData
    {
        public readonly Transform Target;
        public readonly TouchLayer Layer;
        public readonly Component Collider;
        public readonly Camera Camera;
        public readonly Vector2 ScreenPosition;

        public HitData(Transform target, TouchLayer layer, Component collider, Camera camera, Vector2 screenPosition)
        {
            Target = target;
            Layer = layer;
            Collider = collider;
            Camera = camera;
            ScreenPosition = screenPosition;
        }

        public HitData(TouchLayer layer, RaycastResult raycastResult)
        {
            Target = raycastResult.collider.transform;
            Layer = layer;
            Collider = raycastResult.collider;
            Camera = raycastResult.camera;
            ScreenPosition = raycastResult.screenPosition;
        }
    }
}