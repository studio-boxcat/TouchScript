/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TouchScript.InputSources;
using TouchScript.Utils;
using TouchScript.Pointers;
using UnityEngine;
using UnityEngine.Assertions;

namespace TouchScript.Core
{
    /// <summary>
    /// Default implementation of <see cref="ITouchManager"/>.
    /// </summary>
    public sealed class TouchManager : MonoBehaviour, IPointerEventListener
    {
        #region Events

        public event EventHandler FrameStarted;
        public event EventHandler FrameFinished;
        public event EventHandler<PointerEventArgs> PointersAdded;
        public event EventHandler<PointerEventArgs> PointersUpdated;
        public event EventHandler<PointerEventArgs> PointersPressed;
        public event EventHandler<PointerEventArgs> PointersReleased;
        public event EventHandler<PointerEventArgs> PointersRemoved;
        public event EventHandler<PointerEventArgs> PointersCancelled;

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the instance of TouchManager singleton.
        /// </summary>
        public static TouchManager Instance;

        #endregion

        #region Private variables

        [SerializeField, Required, ChildGameObjectsOnly]
        private StandardInput input;

        readonly List<Pointer> _pointers = new(30);
        readonly Dictionary<PointerId, Pointer> _idToPointer = new(30);

        // Upcoming changes
        readonly List<Pointer> _pointersAdded = new();
        readonly List<PointerId> _pointersUpdated = new();
        readonly List<PointerId> _pointersPressed = new();
        readonly List<PointerId> _pointersReleased = new();
        readonly List<PointerId> _pointersRemoved = new();
        readonly List<PointerId> _pointersCancelled = new();

        #endregion

        #region Public methods

        /// <inheritdoc />
        public void CancelPointer(PointerId id, bool shouldReturn)
        {
            if (_idToPointer.TryGetValue(id, out var pointer))
                pointer.InputSource.CancelPointer(pointer, shouldReturn);
        }

        /// <inheritdoc />
        public void CancelPointer(PointerId id)
        {
            CancelPointer(id, false);
        }

        public void CancelAllPointers()
        {
            foreach (var pointer in _pointers)
                pointer.InputSource.CancelPointer(pointer, false);
            foreach (var pointer in _pointersAdded)
                pointer.InputSource.CancelPointer(pointer, false);
        }

        #endregion

        #region Internal methods

        void IPointerEventListener.AddPointer(Pointer pointer)
        {
            Assert.AreNotEqual(PointerId.Invalid, pointer.Id);

            // XXX: AddPointer 는 한프레임에 여러번 불릴 수 있는 것일까? 중복원소가 있더라도 Warning 은 생략함.
            if (_pointersAdded.Contains(pointer))
                return;

            _pointersAdded.Add(pointer);
        }

        void IPointerEventListener.UpdatePointer(Pointer pointer)
        {
            var id = pointer.Id;
            if (IdToPointerWithAddedPointers(id, out _) == false)
                return;

            // XXX: UpdatePointer 는 한프레임에 여러번 불릴 수 있는 것일까? 중복원소가 있더라도 Warning 은 생략함.
            if (_pointersUpdated.Contains(id) == false)
                _pointersUpdated.Add(id);
        }

        static void CheckAndAddPointer(PointerId id, List<PointerId> list)
        {
            if (list.Contains(id))
            {
#if DEBUG
                Debug.LogWarning($"TouchScript > Pointer with id [{((int) id).ToString()}] is requested to more than once this frame.");
#endif
                return;
            }

            list.Add(id);
        }

        void IPointerEventListener.PressPointer(Pointer pointer)
        {
            var id = pointer.Id;

            if (IdToPointerWithAddedPointers(id, out _) == false)
                return;

            CheckAndAddPointer(id, _pointersPressed);
        }

        /// <inheritdoc />
        void IPointerEventListener.ReleasePointer(Pointer pointer)
        {
            var id = pointer.Id;
            pointer.Buttons &= ~Pointer.PointerButtonState.ButtonPressed;

            if (IdToPointerWithAddedPointers(id, out _) == false)
                return;

            CheckAndAddPointer(id, _pointersReleased);
        }

        /// <inheritdoc />
        void IPointerEventListener.RemovePointer(Pointer pointer)
        {
            var id = pointer.Id;

            if (IdToPointerWithAddedPointers(id, out _) == false)
                return;

            CheckAndAddPointer(id, _pointersRemoved);
        }

        /// <inheritdoc />
        void IPointerEventListener.CancelPointer(Pointer pointer)
        {
            var id = pointer.Id;

            if (IdToPointerWithAddedPointers(id, out _) == false)
                return;

            CheckAndAddPointer(id, _pointersCancelled);
        }

        #endregion

        #region Unity

        private void Update()
        {
            foreach (var pointer in _pointers)
                pointer.INTERNAL_FrameStarted();
            input.UpdateInput();
            UpdatePointers();
        }

        #endregion

        #region Private functions

        bool IdToPointer(PointerId id, out Pointer pointer)
        {
            if (_idToPointer.TryGetValue(id, out pointer))
                return true;

#if DEBUG
            Debug.LogWarning($"TouchScript > Id [{id.ToString()}] not found.");
#endif
            return false;
        }

        bool IdToPointerWithAddedPointers(PointerId id, out Pointer pointer)
        {
            if (_idToPointer.TryGetValue(id, out pointer))
                return true;

            if (wasPointerAddedThisFrame(id))
                return true;

#if DEBUG
            Debug.LogWarning($"TouchScript > Id [{id.ToString()}] not found.");
#endif

            return false;
        }

        void UpdateAdded(List<Pointer> pointers)
        {
            foreach (var pointer in pointers)
            {
                _pointers.Add(pointer);
                _idToPointer.Add(pointer.Id, pointer);
            }

            PointersAdded?.InvokeHandleExceptions(this, new PointerEventArgs(pointers));
        }

        private void UpdateUpdated(List<PointerId> pointers)
        {
            var list = PointerListPool.Rent();
            foreach (var id in pointers)
            {
                if (IdToPointer(id, out var pointer) == false)
                    continue;
                list.Add(pointer);
            }
            PointersUpdated?.InvokeHandleExceptions(this, new PointerEventArgs(list));
            PointerListPool.Release(list);
        }

        void UpdatePressed(List<PointerId> pointers)
        {
            var list = PointerListPool.Rent();
            foreach (var id in pointers)
            {
                if (IdToPointer(id, out var pointer) == false)
                    continue;
                list.Add(pointer);

                var hit = pointer.GetOverData();
                pointer.INTERNAL_SetPressData(hit);
            }

            PointersPressed?.InvokeHandleExceptions(this, new PointerEventArgs(list));
            PointerListPool.Release(list);
        }

        void UpdateReleased(List<PointerId> pointers)
        {
            var list = PointerListPool.Rent();
            foreach (var id in pointers)
            {
                if (IdToPointer(id, out var pointer) == false)
                    continue;
                list.Add(pointer);
            }

            PointersReleased?.InvokeHandleExceptions(this, new PointerEventArgs(list));

            foreach (var pointer in list)
                pointer.INTERNAL_ClearPressData();
            PointerListPool.Release(list);
        }

        void UpdateRemoved(List<PointerId> pointers)
        {
            var list = PointerListPool.Rent();
            foreach (var id in pointers)
            {
                if (IdToPointer(id, out var pointer) == false)
                    continue;
                _idToPointer.Remove(id);
                _pointers.Remove(pointer);
                list.Add(pointer);
            }

            PointersRemoved?.InvokeHandleExceptions(this, new PointerEventArgs(list));

            foreach (var pointer in list)
                pointer.InputSource.INTERNAL_DiscardPointer(pointer);
            PointerListPool.Release(list);
        }

        private void UpdateCancelled(List<PointerId> pointers)
        {
            var list = PointerListPool.Rent();
            foreach (var id in pointers)
            {
                if (IdToPointer(id, out var pointer) == false)
                    continue;
                _idToPointer.Remove(id);
                _pointers.Remove(pointer);
                list.Add(pointer);
            }

            PointersCancelled?.InvokeHandleExceptions(this, new PointerEventArgs(list));

            foreach (var pointer in list)
                pointer.InputSource.INTERNAL_DiscardPointer(pointer);
            PointerListPool.Release(list);
        }

        static readonly List<Pointer> _tmpPointersAdded = new();
        static readonly List<PointerId> _tmpPointersUpdated = new();
        static readonly List<PointerId> _tmpPointersPressed = new();
        static readonly List<PointerId> _tmpPointersReleased = new();
        static readonly List<PointerId> _tmpPointersRemoved = new();
        static readonly List<PointerId> _tmpPointersCancelled = new();

        void UpdatePointers()
        {
            FrameStarted?.InvokeHandleExceptions(this, EventArgs.Empty);

            ClearAndPour(_pointersAdded, _tmpPointersAdded);
            ClearAndPour(_pointersUpdated, _tmpPointersUpdated);
            ClearAndPour(_pointersPressed, _tmpPointersPressed);
            ClearAndPour(_pointersReleased, _tmpPointersReleased);
            ClearAndPour(_pointersRemoved, _tmpPointersRemoved);
            ClearAndPour(_pointersCancelled, _tmpPointersCancelled);

            foreach (var pointer in _pointers)
                pointer.INTERNAL_UpdatePosition();

            if (_tmpPointersAdded.Count > 0)
                UpdateAdded(_tmpPointersAdded);
            if (_tmpPointersUpdated.Count > 0)
                UpdateUpdated(_tmpPointersUpdated);
            if (_tmpPointersPressed.Count > 0)
                UpdatePressed(_tmpPointersPressed);
            if (_tmpPointersReleased.Count > 0)
                UpdateReleased(_tmpPointersReleased);
            if (_tmpPointersRemoved.Count > 0)
                UpdateRemoved(_tmpPointersRemoved);
            if (_tmpPointersCancelled.Count > 0)
                UpdateCancelled(_tmpPointersCancelled);

            FrameFinished?.InvokeHandleExceptions(this, EventArgs.Empty);
        }

        static void ClearAndPour<T>(List<T> src, List<T> dst)
        {
            dst.Clear();
            if (src.Count == 0)
                return;
            dst.AddRange(src);
            src.Clear();
        }

        private bool wasPointerAddedThisFrame(PointerId id)
        {
            foreach (var p in _pointersAdded)
            {
                if (p.Id == id)
                    return true;
            }
            return false;
        }

        #endregion

        static class PointerListPool
        {
            static readonly List<Pointer> _list = new(8);
            static bool _usingStaticList;

            public static List<Pointer> Rent()
            {
                if (_usingStaticList == false)
                {
                    _usingStaticList = true;
                    return _list;
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogWarning("List<Pointer> allocation occured.");
#endif
                    return new List<Pointer>();
                }
            }

            public static void Release(List<Pointer> list)
            {
                list.Clear();
                if (_usingStaticList && list == _list)
                    _usingStaticList = false;
            }
        }
    }
}