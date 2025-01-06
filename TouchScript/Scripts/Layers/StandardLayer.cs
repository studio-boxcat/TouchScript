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

        public override HitResult Hit(Vector2 screenPosition, out HitData hit)
        {
            if (QuickRaycast.Raycast(screenPosition, _camera, out var raycastResult))
            {
                hit = new HitData(this, raycastResult);
                return HitResult.Hit;
            }
            else
            {
                hit = default;
                return HitResult.Miss;
            }
        }
    }
}