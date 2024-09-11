/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using TouchScript.Gestures;
using TouchScript.Utils;
using TouchScript.Pointers;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = TouchScript.Utils.Logger;

namespace TouchScript.Core
{
    sealed class GestureManager : MonoBehaviour
    {
        #region Public properties

        /// <summary>
        /// Gets the instance of GestureManager singleton.
        /// </summary>
        public static GestureManager Instance;

        #endregion

        #region Private variables

        [SerializeField, Required, ChildGameObjectsOnly]
        TouchManager _touchManager;

        static readonly Logger _logger = new(nameof(GestureManager));

        // Upcoming changes
        readonly List<Gesture> _gesturesToReset = new(4);
        readonly PointerToGestures _pointerToGestures = new(4);

        #endregion

        #region Temporary collections

        // Temporary collections for update methods.
        // Dictionary<Transform, List<Pointer>> - pointers sorted by targets
        readonly TransformToPointers _pointersOnTarget = new(4);
        readonly GestureToPointers _gestureToPointers = new(4);
        readonly GestureCache _gestureCache = new(4);

        #endregion

        #region Pools

        static readonly ListPool<Gesture> _gestureListPool = new(4);
        static readonly ListPool<Pointer> _pointerListPool = new(4);

        #endregion

        #region Unity

        private void Awake()
        {
            // gameObject.hideFlags = HideFlags.HideInHierarchy;
            // DontDestroyOnLoad(gameObject);

            _touchManager.FrameStarted += ResetGestures;
            _touchManager.FrameFinished += () =>
            {
                ResetGestures();
                _gestureCache.Clear();
            };

            _touchManager.PointerUpdated += updateUpdated;
            _touchManager.PointerPressed += updatePressed;
            _touchManager.PointerReleased += updateReleased;
            _touchManager.PointerCancelled += updateCancelled;
        }

        #endregion

        #region Internal methods

        internal GestureState INTERNAL_GestureChangeState(Gesture gesture, GestureState state)
        {
            bool recognized = false;
            switch (state)
            {
                case GestureState.Idle:
                case GestureState.Possible:
                    break;
                case GestureState.Began:
                    switch (gesture.State)
                    {
                        case GestureState.Idle:
                        case GestureState.Possible:
                            break;
                        default:
                            print(string.Format("Gesture {0} erroneously tried to enter state {1} from state {2}",
                                new object[] { gesture, state, gesture.State }));
                            break;
                    }
                    recognized = recognizeGestureIfNotPrevented(gesture);
                    if (!recognized)
                    {
                        if (!_gesturesToReset.Contains(gesture)) _gesturesToReset.Add(gesture);
                        return GestureState.Failed;
                    }
                    break;
                case GestureState.Changed:
                    switch (gesture.State)
                    {
                        case GestureState.Began:
                        case GestureState.Changed:
                            break;
                        default:
                            print(string.Format("Gesture {0} erroneously tried to enter state {1} from state {2}",
                                new object[] { gesture, state, gesture.State }));
                            break;
                    }
                    break;
                case GestureState.Failed:
                    if (!_gesturesToReset.Contains(gesture)) _gesturesToReset.Add(gesture);
                    break;
                case GestureState.Ended: // Ended
                    if (!_gesturesToReset.Contains(gesture)) _gesturesToReset.Add(gesture);
                    switch (gesture.State)
                    {
                        case GestureState.Idle:
                        case GestureState.Possible:
                            recognized = recognizeGestureIfNotPrevented(gesture);
                            if (!recognized) return GestureState.Failed;
                            break;
                        case GestureState.Began:
                        case GestureState.Changed:
                            break;
                        default:
                            print(string.Format("Gesture {0} erroneously tried to enter state {1} from state {2}",
                                new object[] { gesture, state, gesture.State }));
                            break;
                    }
                    break;
                case GestureState.Cancelled:
                    if (!_gesturesToReset.Contains(gesture)) _gesturesToReset.Add(gesture);
                    break;
            }

            return state;
        }

        #endregion

        #region Private functions

        private void updatePressed(Pointer pointer)
        {
            _pointersOnTarget.EnsureCleared();
            _gestureToPointers.EnsureCleared();

            // Arrange pointers by target.
            {
                var target = pointer.GetPressData().Target;
                if (target != null)
                    _pointersOnTarget.Add(target, pointer);
            }

            var startedGestures = _gestureListPool.Get();
            // Process all targets - get and sort all gestures on targets in hierarchy.
            foreach (var (target, targetPointers) in _pointersOnTarget)
            {
                Assert.AreEqual(0, startedGestures.Count);

                // Gestures on objects in the target.
                var possibleGestures = _gestureCache.GetGesturesOfTarget(target);
                foreach (var gesture in possibleGestures)
                    if (gesture.State.IsBeganOrChanged())
                        startedGestures.Add(gesture);

                foreach (var possibleGesture in possibleGestures)
                {
                    // WARNING! Gesture state might change during this loop.
                    // For example when one of them recognizes.

                    // If the gesture is not active it can't start or recognize.
                    if (!gestureIsActive(possibleGesture)) continue;

                    var canReceivePointers = true;

                    // For every possible gesture in gesturesInHierarchy we need to check if it prevents gestureOnParentOrMe from getting pointers.
                    foreach (var startedGesture in startedGestures)
                    {
                        if (ReferenceEquals(possibleGesture, startedGesture)) continue;

                        // This gesture has started. Is gestureOnParentOrMe allowed to work in parallel?
                        if (startedGesture.CanPreventGesture(possibleGesture))
                        {
                            // activeGesture has already began and prevents gestureOnParentOrMe from getting pointers.
                            canReceivePointers = false;
                            break;
                        }
                    }

                    if (!canReceivePointers) continue;

                    // Filter incoming pointers for gesture.
                    _gestureToPointers.AddReceivablePointersToGesture(possibleGesture, targetPointers);
                }

                startedGestures.Clear();
            }

            // Dispatch gesture events with pointers assigned to them.
            foreach (var (gesture, list) in _gestureToPointers)
            {
                if (!gestureIsActive(gesture))
                    continue;

                _pointerToGestures.AddGestureToPointers(gesture, list);
                gesture.INTERNAL_PointersPressed(list);
            }

            _gestureListPool.Release(startedGestures);
            _pointersOnTarget.Clear();
            _gestureToPointers.Clear();
        }

        private void updateUpdated(Pointer pointer)
        {
            _gestureToPointers.EnsureCleared();
            _gestureToPointers.Add(pointer, _pointerToGestures);
            foreach (var (gesture, list) in _gestureToPointers)
            {
                if (gestureIsActive(gesture))
                    gesture.INTERNAL_PointersUpdated(list);
            }

            _gestureToPointers.Clear();
        }

        private void updateReleased(Pointer pointers)
        {
            _gestureToPointers.EnsureCleared();
            _gestureToPointers.Add(pointers, _pointerToGestures);
            foreach (var (gesture, list) in _gestureToPointers)
            {
                if (gestureIsActive(gesture))
                    gesture.INTERNAL_PointersReleased(list);
            }

            _pointerToGestures.RemovePointers(pointers);
            _gestureToPointers.Clear();
        }

        private void updateCancelled(Pointer pointer)
        {
            _gestureToPointers.EnsureCleared();
            _gestureToPointers.Add(pointer, _pointerToGestures);
            foreach (var (gesture, list) in _gestureToPointers)
            {
                if (gestureIsActive(gesture))
                    gesture.INTERNAL_PointersCancelled(list);
            }

            _pointerToGestures.RemovePointers(pointer);
            _gestureToPointers.Clear();
        }

        private void ResetGestures()
        {
            if (_gesturesToReset.Count == 0) return;

            foreach (var gesture in _gesturesToReset)
            {
                _pointerToGestures.RemoveGestureFromPointers(gesture.activePointers, gesture);

                // Unity "null" comparison
                if (gesture != null)
                {
                    gesture.INTERNAL_Reset();
                    gesture.INTERNAL_SetState(GestureState.Idle);
                }
            }

            _gesturesToReset.Clear();
        }

        static bool gestureIsActive(Gesture gesture)
        {
            if (gesture.gameObject.activeInHierarchy == false) return false;
            if (gesture.enabled == false) return false;
            switch (gesture.State)
            {
                case GestureState.Failed:
                case GestureState.Ended:
                case GestureState.Cancelled:
                    return false;
                default:
                    return true;
            }
        }

        private bool recognizeGestureIfNotPrevented(Gesture gesture)
        {
            var gesturesToFail = _gestureListPool.Get();
            bool canRecognize = true;
            var target = gesture.transform;

            var otherGestures = _gestureCache.GetGesturesOfTarget(target);

            foreach (var otherGesture in otherGestures)
            {
                if (ReferenceEquals(gesture, otherGesture)) continue;
                if (!gestureIsActive(otherGesture)) continue;

                if (otherGesture.State.IsBeganOrChanged())
                {
                    if (otherGesture.CanPreventGesture(gesture))
                    {
                        canRecognize = false;
                        break;
                    }
                }
                else if (otherGesture.State == GestureState.Possible)
                {
                    if (gesture.CanPreventGesture(otherGesture))
                    {
                        gesturesToFail.Add(otherGesture);
                    }
                }
            }

            if (canRecognize)
            {
                foreach (var gestureToFail in gesturesToFail)
                    gestureToFail.INTERNAL_SetState(GestureState.Failed);
            }

            _gestureListPool.Release(gesturesToFail);

            return canRecognize;
        }

        #endregion

        readonly struct GestureToPointers
        {
            readonly List<(Gesture, List<Pointer>)> _list;
            readonly Dictionary<Gesture, List<Pointer>> _dict;

            public GestureToPointers(int capacity)
            {
                _list = new List<(Gesture, List<Pointer>)>(capacity);
                _dict = new Dictionary<Gesture, List<Pointer>>(capacity);
            }

            static readonly List<Pointer> _pointerBuffer = new();

            public void AddReceivablePointersToGesture(Gesture gesture, List<Pointer> pointers)
            {
                Assert.AreEqual(0, _pointerBuffer.Count);

                // 포인터 모으기.
                foreach (var pointer in pointers)
                {
                    if (gesture.ShouldReceivePointer(pointer))
                        _pointerBuffer.Add(pointer);
                }
                if (_pointerBuffer.Count == 0)
                    return;

                // 버퍼에 등록하기.
                if (_dict.TryGetValue(gesture, out var list) == false)
                {
                    list = _pointerListPool.Get();
                    // Add gesture to the list of active gestures this update.
                    _dict[gesture] = list;
                    _list.Add((gesture, list));
                }

                Assert.IsFalse(list.Any(p => _pointerBuffer.Contains(p)));
                list.AddRange(_pointerBuffer);

                _pointerBuffer.Clear();
            }

            public void Add(Pointer pointer, PointerToGestures pointerToGestures)
            {
                if (!pointerToGestures.TryGetValue(pointer.Id, out var gestures))
                    return;

                foreach (var gesture in gestures)
                {
                    if (!_dict.TryGetValue(gesture, out var list))
                    {
                        list = _pointerListPool.Get();
                        _dict.Add(gesture, list);
                        _list.Add((gesture, list));
                    }

                    Assert.IsFalse(list.Contains(pointer));
                    list.Add(pointer);
                }
            }

            public void Clear()
            {
                if (_list.Count == 0)
                    return;
                foreach (var (_, list) in _list)
                    _pointerListPool.Release(list);
                _dict.Clear();
                _list.Clear();
            }

            public void EnsureCleared()
            {
                if (_list.Count > 0)
                {
                    _logger.Error("_gesturesToPointers 가 초기화되지 않은 상태입니다.");
                    Clear();
                }
            }

            public List<(Gesture, List<Pointer>)>.Enumerator GetEnumerator() => _list.GetEnumerator();
        }

        readonly struct PointerToGestures
        {
            readonly Dictionary<PointerId, List<Gesture>> _dict;


            public PointerToGestures(int capacity)
            {
                _dict = new Dictionary<PointerId, List<Gesture>>(capacity);
            }

            public bool TryGetValue(PointerId pointerId, out List<Gesture> gestures)
            {
                return _dict.TryGetValue(pointerId, out gestures);
            }

            public void AddGestureToPointers(Gesture gesture, List<Pointer> pointers)
            {
                foreach (var pointer in pointers)
                {
                    if (!_dict.TryGetValue(pointer.Id, out var list))
                    {
                        list = _gestureListPool.Get();
                        _dict.Add(pointer.Id, list);
                    }

                    Assert.IsFalse(list.Contains(gesture));
                    list.Add(gesture);
                }
            }

            public void RemovePointers(Pointer pointer)
            {
                Assert.IsTrue(pointer.Id.IsValid());

                if (_dict.Remove(pointer.Id, out var list))
                {
                    _gestureListPool.Release(list);
                }
            }

            public void RemoveGestureFromPointers(List<Pointer> pointers, Gesture gesture)
            {
                foreach (var pointer in pointers)
                {
                    if (_dict.TryGetValue(pointer.Id, out var list))
                        list.Remove(gesture);
                }
            }
        }

        readonly struct TransformToPointers
        {
            readonly Dictionary<Transform, List<Pointer>> _dict;
            readonly List<(Transform, List<Pointer>)> _list;

            public TransformToPointers(int capacity)
            {
                _dict = new Dictionary<Transform, List<Pointer>>(capacity);
                _list = new List<(Transform, List<Pointer>)>(capacity);
            }

            public void Add(Transform target, Pointer pointer)
            {
                if (_dict.TryGetValue(target, out var list))
                {
                    Assert.IsFalse(list.Contains(pointer));
                    list.Add(pointer);
                    return;
                }

                list = _pointerListPool.Get();
                list.Add(pointer);
                _dict.Add(target, list);
                _list.Add((target, list));
            }

            public void Clear()
            {
                if (_list.Count == 0)
                    return;
                foreach (var (_, list) in _list)
                    _pointerListPool.Release(list);
                _dict.Clear();
                _list.Clear();
            }

            public void EnsureCleared()
            {
                if (_list.Count > 0)
                {
                    _logger.Error("_gesturesToPointers 가 초기화되지 않은 상태입니다.");
                    Clear();
                }
            }

            public List<(Transform, List<Pointer>)>.Enumerator GetEnumerator() => _list.GetEnumerator();
        }

        readonly struct GestureCache
        {
            readonly Dictionary<Transform, List<Gesture>> _dict;

            public GestureCache(int capacity)
            {
                _dict = new Dictionary<Transform, List<Gesture>>(capacity);
            }

            // target <- child*
            public List<Gesture> GetGesturesOfTarget(Transform target)
            {
                if (_dict.TryGetValue(target, out var list))
                    return list;

                list = _gestureListPool.Get();
                target.GetComponents(list);
                _dict.Add(target, list);

                return list;
            }

            public void Clear()
            {
                foreach (var list in _dict.Values)
                    _gestureListPool.Release(list);
                _dict.Clear();
            }
        }
    }
}