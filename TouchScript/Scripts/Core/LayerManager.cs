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

        static readonly Dictionary<TouchLayer, int> _layerInternalOrder = new();
        static int _nextOrder;
        static bool _sorted = true;

        public static void AddLayer(TouchLayer layer)
        {
            Assert.IsNotNull(layer);
            Assert.IsFalse(_layers.Contains(layer));

            _layers.Add(layer);
            _layerInternalOrder.Add(layer, _nextOrder++);

            if (_layers.Count > 1 && layer.Priority > _layers[^2].Priority)
                _sorted = false;
        }

        public static void RemoveLayer(TouchLayer layer)
        {
            _layers.Remove(layer);
            _layerInternalOrder.Remove(layer);
        }

        public static bool GetHitTarget(Vector2 screenPosition, out HitData hit)
        {
            if (_sorted == false)
            {
                _layers.Sort((a, b) =>
                {
                    return a.Priority != b.Priority
                        ? a.Priority.CompareTo(b.Priority)
                        : _layerInternalOrder[a].CompareTo(_layerInternalOrder[b]);
                });

                _sorted = true;
            }

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