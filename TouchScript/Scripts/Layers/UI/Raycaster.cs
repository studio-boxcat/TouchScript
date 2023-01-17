using System.Collections.Generic;
using JetBrains.Annotations;
using TouchScript.Hit;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TouchScript.Layers.UI
{
    public static class Raycaster
    {
        static readonly List<BaseRaycaster> _raycasters;
        static readonly Dictionary<int, Canvas> _canvasCache = new(10);

        static Raycaster()
        {
            _raycasters = RaycasterManager.GetRaycasters();
        }

        /// <summary>
        /// Returns all UI raycasters in the scene.
        /// </summary>
        /// <returns> Array of raycasters. </returns>
        public static List<BaseRaycaster> GetRaycasters()
        {
            return _raycasters;
        }

        /// <summary>
        /// Returns a Canvas for a raycaster.
        /// </summary>
        /// <param name="raycaster">The raycaster.</param>
        /// <returns> The Canvas this raycaster is on. </returns>
        [NotNull]
        public static Canvas GetCanvasForRaycaster(BaseRaycaster raycaster)
        {
            var id = raycaster.GetInstanceID();

            return _canvasCache.TryGetValue(id, out var canvas)
                ? canvas : (_canvasCache[id] = raycaster.GetComponent<Canvas>());
        }

        static readonly List<RaycastResult> _raycastResultBuffer = new(16);

        public static void Raycast(GraphicRaycaster raycaster, Vector2 screenPosition, Canvas canvas, List<RaycastHitUI> result)
        {
            Assert.AreEqual(0, result.Count);

            raycaster.Raycast(screenPosition, _raycastResultBuffer);
            if (_raycastResultBuffer.Count == 0)
                return;

            foreach (var raycastResult in _raycastResultBuffer)
            {
                var trans = raycastResult.gameObject.transform;
                var graphic = raycastResult.gameObject.GetComponent<Graphic>();
                result.Add(new RaycastHitUI(
                    trans,
                    graphic,
                    raycaster,
                    result.Count,
                    graphic.depth,
                    canvas.sortingLayerID,
                    canvas.sortingOrder));
            }

            _raycastResultBuffer.Clear();
        }
    }
}