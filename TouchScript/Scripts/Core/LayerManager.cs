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
            // XXX: 터치를 막고있는 경우, Raycast 를 무조건 실패시킴.
            if (TouchManager.Instance.enabled == false)
            {
                hit = default;
                return false;
            }

            EnsureSorted();

            foreach (var layer in _layers)
            {
                Assert.IsTrue(layer.isActiveAndEnabled);
                var hitResult = layer.Hit(screenPosition, out hit);
                if (hitResult == HitResult.Hit) return true;
            }

            hit = new HitData(null, null, null, null, screenPosition);
            return false;
        }

        static void EnsureSorted()
        {
            if (_sorted) return;

            _layers.Sort((a, b) =>
            {
                return a.Priority != b.Priority
                    ? a.Priority.CompareTo(b.Priority)
                    : _layerInternalOrder[a].CompareTo(_layerInternalOrder[b]);
            });

            _sorted = true;
        }
    }
}