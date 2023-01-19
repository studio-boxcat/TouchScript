/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using TouchScript.Layers;
using UnityEngine;

namespace TouchScript.Hit
{
    /// <summary>
    /// An object representing a point hit by a pointer in 3D, 2D or UI space.
    /// </summary>
    public readonly struct HitData
    {
        public readonly Transform Target;
        public readonly TouchLayer Layer;
        public readonly RaycastHitUI RaycastHitUI;

        public HitData(Transform target, TouchLayer layer, RaycastHitUI raycastHitUI)
        {
            Target = target;
            Layer = layer;
            RaycastHitUI = raycastHitUI;
        }

        public bool IsNotUI() => RaycastHitUI.Target is null;
    }
}