/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using TouchScript.Layers;
using UnityEngine;
using UnityEngine.UI;

namespace TouchScript.Hit
{
    /// <summary>
    /// An object representing a point hit by a pointer in 3D, 2D or UI space.
    /// </summary>
    public readonly struct HitData
    {
        public readonly Transform Target;
        public readonly TouchLayer Layer;
        public readonly Graphic Graphic;
        public readonly GraphicRaycaster GraphicRaycaster;
        public readonly Vector2 ScreenPosition;

        public HitData(Transform target, TouchLayer layer, Graphic graphic, GraphicRaycaster graphicRaycaster, Vector2 screenPosition)
        {
            Target = target;
            Layer = layer;
            Graphic = graphic;
            GraphicRaycaster = graphicRaycaster;
            ScreenPosition = screenPosition;
        }
    }
}