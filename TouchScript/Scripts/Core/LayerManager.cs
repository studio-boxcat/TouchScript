/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using TouchScript.Hit;
using TouchScript.Layers;
using UnityEngine;
using UnityEngine.Assertions;

namespace TouchScript.Core
{
    public static class LayerManager
    {
        static readonly List<TouchLayer> _layers = new(10);

        public static void AddLayer(TouchLayer layer, int index = -1)
        {
            Assert.IsNotNull(layer);
            Assert.IsFalse(_layers.Contains(layer));

            if (index == -1 || index >= _layers.Count)
            {
                _layers.Add(layer);
            }
            else
            {
                _layers.Insert(index, layer);
            }
        }

        public static void RemoveLayer(TouchLayer layer)
        {
            _layers.Remove(layer);
        }

        public static bool GetHitTarget(Vector2 screenPosition, out HitData hit)
        {
            foreach (var layer in _layers)
            {
                var hitResult = layer.Hit(screenPosition, out hit);
                if (hitResult == HitResult.Hit) return true;
            }

            hit = default;
            return false;
        }
    }
}