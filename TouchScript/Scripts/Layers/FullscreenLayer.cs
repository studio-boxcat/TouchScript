/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using TouchScript.Hit;
using UnityEngine;

namespace TouchScript.Layers
{
    /// <summary>
    /// Layer which gets all input from a camera. Should be used instead of a background object getting all the pointers which come through.
    /// </summary>
    [AddComponentMenu("TouchScript/Layers/Fullscreen Layer")]
    [HelpURL("http://touchscript.github.io/docs/html/T_TouchScript_Layers_FullscreenLayer.htm")]
    public class FullscreenLayer : TouchLayer
    {
        Camera _camera;

        public override ProjectionParams GetProjectionParams()
        {
            _camera ??= Camera.main;
            return new ProjectionParams(_camera);
        }

        public override HitResult Hit(Vector2 screenPosition, out HitData hit)
        {
            if (base.Hit(screenPosition, out hit) != HitResult.Hit) return HitResult.Miss;

            hit = new HitData(transform, this, default, default, screenPosition);
            return HitResult.Hit;
        }
    }
}