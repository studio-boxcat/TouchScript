/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using TouchScript.Hit;
using TouchScript.Pointers;
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
        #region Public properties

        /// <inheritdoc />
        public override string Name => "Global Fullscreen";

        /// <inheritdoc />
        public override Vector3 WorldProjectionNormal => transform.forward;

        #endregion

        #region Public methods

        /// <inheritdoc />
        public override HitResult Hit(Vector2 screenPosition, out HitData hit)
        {
            if (base.Hit(screenPosition, out hit) != HitResult.Hit) return HitResult.Miss;

            hit = new HitData(transform, this);
            return HitResult.Hit;
        }

        #endregion
    }
}