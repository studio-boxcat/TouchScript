using TouchScript.Hit;
using UnityEngine;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;

namespace TouchScript.Layers
{
    public class StandardLayer : TouchLayer
    {
        [SerializeField, Required, ChildGameObjectsOnly]
        private Camera _camera;

        public override Camera GetTargetCamera() => _camera;

        public override RaycastResultType Hit(Vector2 screenPosition, out HitData hit)
        {
            var result = QuickRaycast.Raycast(screenPosition, _camera, out var raycastResult);
            hit = result is RaycastResultType.Hit ? new HitData(this, raycastResult) : default;
            return result;
        }
    }
}