using TouchScript.Hit;
using UnityEngine;
using TouchScript.Core;
using UnityEngine.EventSystems;

namespace TouchScript.Layers
{
    public abstract class TouchLayer : MonoBehaviour
    {
        [SerializeField]
        private int _priority;
        public int Priority => _priority;

        private void OnEnable() => LayerManager.AddLayer(this);
        private void OnDisable() => LayerManager.RemoveLayer(this);

        public abstract Camera GetTargetCamera();
        public abstract RaycastResultType Hit(Vector2 screenPosition, out HitData hit);
    }
}