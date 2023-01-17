/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TouchScript.Hit
{
    /// <exclude />
    public struct RaycastHitUI
    {
        public Transform Target;
        public Graphic Graphic;
        public GraphicRaycaster Raycaster;
        public int GraphicIndex;
        public int Depth;
        public int SortingLayer;
        public int SortingOrder;

        public RaycastHitUI(Transform target, Graphic graphic, GraphicRaycaster raycaster, int graphicIndex, int depth, int sortingLayer, int sortingOrder)
        {
            Target = target;
            Graphic = graphic;
            Raycaster = raycaster;
            GraphicIndex = graphicIndex;
            Depth = depth;
            SortingLayer = sortingLayer;
            SortingOrder = sortingOrder;
        }
    }

    public class RaycastHitUIComparer : IComparer<RaycastHitUI>
    {
        public static readonly RaycastHitUIComparer Instance = new();

        public int Compare(RaycastHitUI lhs, RaycastHitUI rhs)
        {
            if (lhs.SortingLayer != rhs.SortingLayer)
            {
                // Uses the layer value to properly compare the relative order of the layers.
                var rid = SortingLayer.GetLayerValueFromID(rhs.SortingLayer);
                var lid = SortingLayer.GetLayerValueFromID(lhs.SortingLayer);
                return rid.CompareTo(lid);
            }

            if (lhs.SortingOrder != rhs.SortingOrder)
                return rhs.SortingOrder.CompareTo(lhs.SortingOrder);

            if (lhs.Depth != rhs.Depth)
                return rhs.Depth.CompareTo(lhs.Depth);

            return lhs.GraphicIndex.CompareTo(rhs.GraphicIndex);
        }
    }
}