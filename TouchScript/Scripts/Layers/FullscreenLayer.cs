using System;
using TouchScript.Hit;
using UnityEngine;

namespace TouchScript.Layers
{
    public class FullscreenLayer : TouchLayer
    {
        [NonSerialized]
        private Camera _camera;

        public override Camera GetTargetCamera() => _camera ??= Camera.main;

        public override HitResult Hit(Vector2 screenPosition, out HitData hit)
        {
            hit = new HitData(transform, this, default, default, screenPosition);
            return HitResult.Hit;
        }
    }
}