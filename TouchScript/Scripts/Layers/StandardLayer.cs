/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using TouchScript.Hit;
using UnityEngine;
using System.Collections.Generic;
using TouchScript.Layers.UI;
using TouchScript.Pointers;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
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
        #region Public properties

        /// <inheritdoc />
        public override Vector3 WorldProjectionNormal
        {
            get
            {
                if (_camera == null) return Vector3.forward;
                return _camera.transform.forward;
            }
        }

        #endregion

        #region Private variables

        private static readonly Comparison<RaycastHitUI> _raycastHitUIComparerFunc = raycastHitUIComparerFunc;
        private static readonly Comparison<HitData> _hitDataComparerFunc = hitDataComparerFunc;

        private static Dictionary<int, ProjectionParams> projectionParamsCache = new Dictionary<int, ProjectionParams>();
        private static List<BaseRaycaster> raycasters;

        private static List<RaycastHitUI> raycastHitUIList = new List<RaycastHitUI>(20);
        private static List<HitData> hitList = new List<HitData>(20);
        private static RaycastHit2D[] raycastHits2D = new RaycastHit2D[20];

        [SerializeField, Required, ChildGameObjectsOnly]
        Camera _camera;

        [SerializeField]
        [ToggleLeft]
        private bool hit2DObjects = true;

        [SerializeField]
        [ToggleLeft]
        private bool hitWorldSpaceUI = true;

        [SerializeField]
        [ToggleLeft]
        private bool hitScreenSpaceUI = true;

        [SerializeField]
        private LayerMask layerMask = -1;

        private bool lookForCameraObjects = false;
        private TouchScriptInputModule inputModule;

        #endregion

        #region Public methods

        /// <inheritdoc />
        public override HitResult Hit(Vector2 screenPosition, out HitData hit)
        {
            if (base.Hit(screenPosition, out hit) != HitResult.Hit) return HitResult.Miss;

            var result = HitResult.Miss;

            if (hitScreenSpaceUI)
            {
                result = performSSUISearch(screenPosition, out hit);
                switch (result)
                {
                    case HitResult.Hit:
                        return result;
                    case HitResult.Discard:
                        hit = default(HitData);
                        return result;
                }
            }

            if (lookForCameraObjects)
            {
                result = performWorldSearch(screenPosition, out hit);
                switch (result)
                {
                    case HitResult.Hit:
                        return result;
                    case HitResult.Discard:
                        hit = default(HitData);
                        return result;
                }
            }

            return HitResult.Miss;
        }

        /// <inheritdoc />
        public override ProjectionParams GetProjectionParams(Pointer pointer)
        {
            var press = pointer.GetPressData();
            if (press.Type == HitData.HitType.World2D)
                return layerProjectionParams;

            var graphic = press.RaycastHitUI.Graphic;
            if (graphic == null) return layerProjectionParams;
            var canvas = graphic.canvas;
            if (canvas == null) return layerProjectionParams;

            ProjectionParams pp;
            if (!projectionParamsCache.TryGetValue(canvas.GetInstanceID(), out pp))
            {
                // TODO: memory leak
                pp = new WorldSpaceCanvasProjectionParams(canvas);
                projectionParamsCache.Add(canvas.GetInstanceID(), pp);
            }
            return pp;
        }

        #endregion

        #region Unity methods

        /// <inheritdoc />
        protected override void Awake()
        {
            updateVariants();
            base.Awake();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;

            var touchManager = TouchManager.Instance;
            if (touchManager != null) touchManager.FrameStarted += frameStartedHandler;

            StartCoroutine(lateEnable());
        }

        private IEnumerator lateEnable()
        {
            // Need to wait while EventSystem initializes
            yield return new WaitForEndOfFrame();
            setupInputModule();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            if (inputModule != null) 
            {
                inputModule.INTERNAL_Release();
                inputModule = null;
            }

            var touchManager = TouchManager.Instance;
            if (touchManager != null) touchManager.FrameStarted -= frameStartedHandler;
        }

        #endregion

        #region Protected functions

        /// <inheritdoc />
        protected override ProjectionParams createProjectionParams()
        {
            return new CameraProjectionParams(_camera);
        }

        #endregion

        #region Private functions

        private void setupInputModule()
        {
            if (inputModule == null)
            {
                if (!hitWorldSpaceUI && !hitScreenSpaceUI) return;
                inputModule = TouchScriptInputModule.Instance;
                if (inputModule != null) TouchScriptInputModule.Instance.INTERNAL_Retain();
            }
            else
            {
                if (hitWorldSpaceUI || hitScreenSpaceUI) return;
                inputModule.INTERNAL_Release();
                inputModule = null;
            }
        }

        private HitResult performWorldSearch(Vector2 screenPosition, out HitData hit)
        {
            hit = default(HitData);

            if ((_camera.enabled == false) || (_camera.gameObject.activeInHierarchy == false)) return HitResult.Miss;
            if (!_camera.pixelRect.Contains(screenPosition)) return HitResult.Miss;

            hitList.Clear();
            var ray = _camera.ScreenPointToRay(screenPosition);

            int count;

            if (hit2DObjects)
            {
                count = Physics2D.GetRayIntersectionNonAlloc(ray, raycastHits2D, float.MaxValue, layerMask);
                for (var i = 0; i < count; i++)
                {
                    var raycast = raycastHits2D[i];
                    hitList.Add(new HitData(raycast, this));
                }
            }

            if (hitWorldSpaceUI)
            {
                raycastHitUIList.Clear();
                if (raycasters == null) raycasters = TouchScriptInputModule.Instance.GetRaycasters();
                count = raycasters.Count;

                for (var i = 0; i < count; i++)
                {
                    var raycaster = raycasters[i] as GraphicRaycaster;
                    if (raycaster == null) continue;
                    var canvas = TouchScriptInputModule.Instance.GetCanvasForRaycaster(raycaster);
                    if ((canvas == null) || (canvas.renderMode == RenderMode.ScreenSpaceOverlay) || (canvas.worldCamera != _camera)) continue;
                    performUISearchForCanvas(screenPosition, canvas, raycaster, _camera, float.MaxValue, ray);
                }

                count = raycastHitUIList.Count;
                for (var i = 0; i < count; i++) hitList.Add(new HitData(raycastHitUIList[i], this));
            }

            count = hitList.Count;
            if (hitList.Count == 0) return HitResult.Miss;
            if (count > 1)
            {
                hitList.Sort(_hitDataComparerFunc);
                {
                    hit = hitList[0];
                    return HitResult.Hit;
                }
            }
            hit = hitList[0];
            return HitResult.Hit;
        }

        private HitResult performSSUISearch(Vector2 screenPosition, out HitData hit)
        {
            hit = default(HitData);
            raycastHitUIList.Clear();

            if (raycasters == null) raycasters = TouchScriptInputModule.Instance.GetRaycasters();
            var count = raycasters.Count;

            for (var i = 0; i < count; i++)
            {
                var raycaster = raycasters[i] as GraphicRaycaster;
                if (raycaster == null) continue;
                var canvas = TouchScriptInputModule.Instance.GetCanvasForRaycaster(raycaster);
                if ((canvas == null) || (canvas.renderMode != RenderMode.ScreenSpaceOverlay)) continue;
                performUISearchForCanvas(screenPosition, canvas, raycaster);
            }

            count = raycastHitUIList.Count;
            if (count == 0) return HitResult.Miss;
            if (count > 1)
            {
                raycastHitUIList.Sort(_raycastHitUIComparerFunc);

                hit = new HitData(raycastHitUIList[0], this, true);
                return HitResult.Hit;
            }

            hit = new HitData(raycastHitUIList[0], this, true);
            return HitResult.Hit;
        }

        private static readonly List<RaycastResult> _raycastResultBuffer = new List<RaycastResult>(16);

        private void performUISearchForCanvas(Vector2 screenPosition, Canvas canvas, GraphicRaycaster raycaster, Camera eventCamera = null, float maxDistance = float.MaxValue, Ray ray = default(Ray))
        {
            var eventData = new PointerEventData() {position = screenPosition};
            raycaster.Raycast(eventData, _raycastResultBuffer);
            foreach (var result in _raycastResultBuffer)
            {
                var trans = result.gameObject.transform;

                var graphic = result.gameObject.GetComponent<Graphic>();
                raycastHitUIList.Add(
                    new RaycastHitUI()
                    {
                        Target = trans,
                        Raycaster = raycaster,
                        Graphic = graphic,
                        GraphicIndex = raycastHitUIList.Count,
                        Depth = graphic.depth,
                        SortingLayer = canvas.sortingLayerID,
                        SortingOrder = canvas.sortingOrder,
                        Distance = result.distance
                    });
            }
            _raycastResultBuffer.Clear();
        }

        private void updateVariants()
        {
            lookForCameraObjects = _camera != null && (hit2DObjects || hitWorldSpaceUI);
        }

        #endregion

        #region Compare functions

        private static int raycastHitUIComparerFunc(RaycastHitUI lhs, RaycastHitUI rhs)
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

            if (!Mathf.Approximately(lhs.Distance, rhs.Distance))
                return lhs.Distance.CompareTo(rhs.Distance);

            return lhs.GraphicIndex.CompareTo(rhs.GraphicIndex);
        }

        private static int hitDataComparerFunc(HitData lhs, HitData rhs)
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

            if ((lhs.Type == HitData.HitType.UI) && (rhs.Type == HitData.HitType.UI))
            {
                if (lhs.RaycastHitUI.Depth != rhs.RaycastHitUI.Depth)
                    return rhs.RaycastHitUI.Depth.CompareTo(lhs.RaycastHitUI.Depth);

                if (!Mathf.Approximately(lhs.Distance, rhs.Distance))
                    return lhs.Distance.CompareTo(rhs.Distance);

                if (lhs.RaycastHitUI.GraphicIndex != rhs.RaycastHitUI.GraphicIndex)
                    return rhs.RaycastHitUI.GraphicIndex.CompareTo(lhs.RaycastHitUI.GraphicIndex);
            }

            return lhs.Distance < rhs.Distance ? -1 : 1;
        }

        #endregion

        #region Event handlers

        private void frameStartedHandler(object sender, EventArgs eventArgs)
        {
            raycasters = null;
        }

        #endregion
    }
}