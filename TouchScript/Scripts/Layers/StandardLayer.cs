/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using TouchScript.Hit;
using UnityEngine;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;

namespace TouchScript.Layers
{
    /// <summary>
    /// A layer which combines all types of hit recognition into one: UI (Screen Space and World), 3D and 2D.
    /// </summary>
    /// <seealso cref="TouchScript.Layers.TouchLayer" />
    [AddComponentMenu("TouchScript/Layers/Standard Layer")]
    [HelpURL("http://touchscript.github.io/docs/html/T_TouchScript_Layers_StandardLayer.htm")]
    public class StandardLayer : TouchLayer
    {
        #region Private variables

        [SerializeField, Required, ChildGameObjectsOnly]
        Camera _camera;

        #endregion

        #region Public methods

        /// <inheritdoc />
        public override HitResult Hit(Vector2 screenPosition, out HitData hit)
        {
            if (base.Hit(screenPosition, out hit) != HitResult.Hit)
                return HitResult.Miss;

            if (QuickRaycast.Raycast(screenPosition, _camera, out var raycastResult))
            {
                hit = new HitData(raycastResult.graphic.transform, this, raycastResult.graphic, raycastResult.module);
                return HitResult.Hit;
            }
            else
            {
                hit = default;
                return HitResult.Miss;
            }
        }

        /// <inheritdoc />
        public override ProjectionParams GetProjectionParams() => new(_camera);

        #endregion
    }
}