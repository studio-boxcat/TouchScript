/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using TouchScript.Hit;
using UnityEngine;
using System.Collections.Generic;
using TouchScript.Layers.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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

        static readonly List<RaycastHitUI> _hitList = new(20);

        [SerializeField, Required, ChildGameObjectsOnly]
        Camera _camera;

        #endregion

        #region Public methods

        /// <inheritdoc />
        public override HitResult Hit(Vector2 screenPosition, out HitData hit)
        {
            if (base.Hit(screenPosition, out hit) != HitResult.Hit)
                return HitResult.Miss;

            _hitList.Clear();
            performWorldSearch(screenPosition, _hitList);

            var count = _hitList.Count;
            if (count == 0) return HitResult.Miss;

            if (count > 1)
                _hitList.Sort(RaycastHitUIComparer.Instance);
            var hitUI = _hitList[0];
            hit = new HitData(hitUI.Target, this, hitUI);
            return HitResult.Hit;
        }

        /// <inheritdoc />
        public override ProjectionParams GetProjectionParams() => new(_camera);

        #endregion

        #region Private functions

        private void performWorldSearch(Vector2 screenPosition, List<RaycastHitUI> result)
        {
            if (_camera.enabled == false || _camera.gameObject.activeInHierarchy == false) return;
            if (!_camera.pixelRect.Contains(screenPosition)) return;

            var raycasters = Raycaster.GetRaycasters();

            foreach (GraphicRaycaster raycaster in raycasters)
            {
                var canvas = Raycaster.GetCanvasForRaycaster(raycaster);
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) continue;
                if (ReferenceEquals(canvas.worldCamera, _camera) == false) continue;
                performUISearchForCanvas(screenPosition, canvas, raycaster, result);
            }
        }

        static readonly List<RaycastResult> _raycastResultBuffer = new(16);

        static void performUISearchForCanvas(Vector2 screenPosition, Canvas canvas, GraphicRaycaster raycaster, List<RaycastHitUI> result)
        {
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
                    _hitList.Count,
                    graphic.depth,
                    canvas.sortingLayerID,
                    canvas.sortingOrder));
            }
            _raycastResultBuffer.Clear();
        }

        #endregion
    }
}