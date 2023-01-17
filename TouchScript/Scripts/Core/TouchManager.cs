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
using UnityEngine.Serialization;

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

        bool isInsidePointerFrame;

        #endregion

        #region Private variables

        [FormerlySerializedAs("inputs")]
        [SerializeField, Required, ChildGameObjectsOnly]
        private StandardInput input;

        private List<Pointer> pointers = new(30);
        private HashSet<Pointer> pressedPointers = new();
        private Dictionary<int, Pointer> idToPointer = new(30);

        // Upcoming changes
        private List<Pointer> pointersAdded = new(10);
        private HashSet<int> pointersUpdated = new();
        private HashSet<int> pointersPressed = new();
        private HashSet<int> pointersReleased = new();
        private HashSet<int> pointersRemoved = new();
        private HashSet<int> pointersCancelled = new();

        private static ObjectPool<List<Pointer>> pointerListPool = new(2,
            () => new List<Pointer>(10), null, (l) => l.Clear());

        private static ObjectPool<List<int>> intListPool = new(3, () => new List<int>(10), null,
            (l) => l.Clear());

        private int nextPointerId = 0;

        #endregion

        #region Public methods

        /// <inheritdoc />
        public void CancelPointer(int id, bool shouldReturn)
        {
            if (idToPointer.TryGetValue(id, out var pointer))
                pointer.InputSource.CancelPointer(pointer, shouldReturn);
        }

        /// <inheritdoc />
        public void CancelPointer(int id)
        {
            CancelPointer(id, false);
        }

        public void CancelAllPointers()
        {
            foreach (var pointer in pointers)
                pointer.InputSource.CancelPointer(pointer, false);
            foreach (var pointer in pointersAdded)
                pointer.InputSource.CancelPointer(pointer, false);
        }

        #endregion

        #region Internal methods

        void IPointerEventListener.AddPointer(Pointer pointer)
        {
            {
                pointer.INTERNAL_Init(nextPointerId);
                pointersAdded.Add(pointer);

                nextPointerId++;
            }
        }

        void IPointerEventListener.UpdatePointer(Pointer pointer)
        {
            var id = pointer.Id;

            {
                if (!idToPointer.ContainsKey(id))
                {
                    // This pointer was added this frame
                    if (!wasPointerAddedThisFrame(id))
                    {
                        // No pointer with such id
#if DEBUG
                        Debug.LogWarning("TouchScript > Pointer with id [" + id + "] is requested to MOVE to but no pointer with such id found.");
#endif
                        return;
                    }
                }

                pointersUpdated.Add(id);
            }
        }

        void IPointerEventListener.PressPointer(Pointer pointer)
        {
            var id = pointer.Id;

            {
                if (!idToPointer.ContainsKey(id))
                {
                    // This pointer was added this frame
                    if (!wasPointerAddedThisFrame(id))
                    {
                        // No pointer with such id
#if DEBUG
                        Debug.LogWarning("TouchScript > Pointer with id [" + id +
                                         "] is requested to PRESS but no pointer with such id found.");
#endif
                        return;
                    }
                }
#if DEBUG
                if (!pointersPressed.Add(id))
                    Debug.LogWarning("TouchScript > Pointer with id [" + id +
                                     "] is requested to PRESS more than once this frame.");
#else
                pointersPressed.Add(id);
#endif

            }
        }

        /// <inheritdoc />
        void IPointerEventListener.ReleasePointer(Pointer pointer)
        {
            var id = pointer.Id;
            pointer.Buttons &= ~Pointer.PointerButtonState.AnyButtonPressed;

            {
                if (!idToPointer.ContainsKey(id))
                {
                    // This pointer was added this frame
                    if (!wasPointerAddedThisFrame(id))
                    {
                        // No pointer with such id
#if DEBUG
                        Debug.LogWarning("TouchScript > Pointer with id [" + id +
                                         "] is requested to END but no pointer with such id found.");
#endif
                        return;
                    }
                }
#if DEBUG
                if (!pointersReleased.Add(id))
                    Debug.LogWarning("TouchScript > Pointer with id [" + id +
                                     "] is requested to END more than once this frame.");
#else
                pointersReleased.Add(id);
#endif

            }
        }

        /// <inheritdoc />
        void IPointerEventListener.RemovePointer(Pointer pointer)
        {
            var id = pointer.Id;

            {
                if (!idToPointer.ContainsKey(id))
                {
                    // This pointer was added this frame
                    if (!wasPointerAddedThisFrame(id))
                    {
                        // No pointer with such id
#if DEBUG
                        Debug.LogWarning("TouchScript > Pointer with id [" + id +
                                         "] is requested to REMOVE but no pointer with such id found.");
#endif
                        return;
                    }
                }
#if DEBUG
                if (!pointersRemoved.Add(pointer.Id))
                    Debug.LogWarning("TouchScript > Pointer with id [" + id +
                                     "] is requested to REMOVE more than once this frame.");
#else
                pointersRemoved.Add(pointer.Id);
#endif

            }
        }

        /// <inheritdoc />
        void IPointerEventListener.CancelPointer(Pointer pointer)
        {
            var id = pointer.Id;

            {
                if (!idToPointer.ContainsKey(id))
                {
                    // This pointer was added this frame
                    if (!wasPointerAddedThisFrame(id))
                    {
                        // No pointer with such id
#if DEBUG
                        Debug.LogWarning("TouchScript > Pointer with id [" + id +
                                         "] is requested to CANCEL but no pointer with such id found.");
#endif
                        return;
                    }
                }
#if DEBUG
                if (!pointersCancelled.Add(pointer.Id))
                    Debug.LogWarning("TouchScript > Pointer with id [" + id +
                                     "] is requested to CANCEL more than once this frame.");
#else
                pointersCancelled.Add(pointer.Id);
#endif

            }
        }

        #endregion

        #region Unity

        private void Awake()
        {
            pointerListPool.WarmUp(2);
            intListPool.WarmUp(3);
        }

        private void Update()
        {
            sendFrameStartedToPointers();
            input.UpdateInput();
            updatePointers();
        }

        public void ForceUpdate() => Update();

        #endregion

        #region Private functions

        private void updateAdded(List<Pointer> pointers)
        {
            var addedCount = pointers.Count;
            var list = pointerListPool.Get();
            for (var i = 0; i < addedCount; i++)
            {
                var pointer = pointers[i];
                list.Add(pointer);
                this.pointers.Add(pointer);
                idToPointer.Add(pointer.Id, pointer);
            }

            PointersAdded?.InvokeHandleExceptions(this, new PointerEventArgs(list));
            pointerListPool.Release(list);
        }

        private void updateUpdated(List<int> pointers)
        {
            var updatedCount = pointers.Count;
            var list = pointerListPool.Get();
            for (var i = 0; i < updatedCount; i++)
            {
                var id = pointers[i];
                if (!idToPointer.TryGetValue(id, out var pointer))
                {
#if DEBUG
                    Debug.LogWarning("TouchScript > Id [" + id +
                                     "] was in UPDATED list but no pointer with such id found.");
#endif
                    continue;
                }
                list.Add(pointer);
            }

            PointersUpdated?.InvokeHandleExceptions(this, new PointerEventArgs(list));
            pointerListPool.Release(list);
        }

        private void updatePressed(List<int> pointers)
        {
            var pressedCount = pointers.Count;
            var list = pointerListPool.Get();
            for (var i = 0; i < pressedCount; i++)
            {
                var id = pointers[i];
                if (!idToPointer.TryGetValue(id, out var pointer))
                {
#if DEBUG
                    Debug.LogWarning("TouchScript > Id [" + id +
                                     "] was in PRESSED list but no pointer with such id found.");
#endif
                    continue;
                }
                list.Add(pointer);
                pressedPointers.Add(pointer);

                var hit = pointer.GetOverData();
                pointer.INTERNAL_SetPressData(hit);
            }

            PointersPressed?.InvokeHandleExceptions(this, new PointerEventArgs(list));
            pointerListPool.Release(list);
        }

        private void updateReleased(List<int> pointers)
        {
            var releasedCount = pointers.Count;
            var list = pointerListPool.Get();
            for (var i = 0; i < releasedCount; i++)
            {
                var id = pointers[i];
                if (!idToPointer.TryGetValue(id, out var pointer))
                {
#if DEBUG
                    Debug.LogWarning("TouchScript > Id [" + id + "] was in RELEASED list but no pointer with such id found.");
#endif
                    continue;
                }
                list.Add(pointer);
                pressedPointers.Remove(pointer);
            }

            PointersReleased?.InvokeHandleExceptions(this, new PointerEventArgs(list));

            releasedCount = list.Count;
            for (var i = 0; i < releasedCount; i++)
            {
                var pointer = list[i];
                pointer.INTERNAL_ClearPressData();
            }
            pointerListPool.Release(list);
        }

        private void updateRemoved(List<int> pointers)
        {
            var removedCount = pointers.Count;
            var list = pointerListPool.Get();
            for (var i = 0; i < removedCount; i++)
            {
                var id = pointers[i];
                if (!idToPointer.TryGetValue(id, out var pointer))
                {
#if DEBUG
                    Debug.LogWarning("TouchScript > Id [" + id + "] was in REMOVED list but no pointer with such id found.");
#endif
                    continue;
                }
                idToPointer.Remove(id);
                this.pointers.Remove(pointer);
                pressedPointers.Remove(pointer);
                list.Add(pointer);
            }

            PointersRemoved?.InvokeHandleExceptions(this, new PointerEventArgs(list));

            removedCount = list.Count;
            for (var i = 0; i < removedCount; i++)
            {
                var pointer = list[i];
                pointer.InputSource.INTERNAL_DiscardPointer(pointer);
            }
            pointerListPool.Release(list);
        }

        private void updateCancelled(List<int> pointers)
        {
            var cancelledCount = pointers.Count;
            var list = pointerListPool.Get();
            for (var i = 0; i < cancelledCount; i++)
            {
                var id = pointers[i];
                if (!idToPointer.TryGetValue(id, out var pointer))
                {
#if DEBUG
                    Debug.LogWarning("TouchScript > Id [" + id +
                                     "] was in CANCELLED list but no pointer with such id found.");
#endif
                    continue;
                }
                idToPointer.Remove(id);
                this.pointers.Remove(pointer);
                pressedPointers.Remove(pointer);
                list.Add(pointer);
            }

            PointersCancelled?.InvokeHandleExceptions(this, new PointerEventArgs(list));

            for (var i = 0; i < cancelledCount; i++)
            {
                var pointer = list[i];
                pointer.InputSource.INTERNAL_DiscardPointer(pointer);
            }
            pointerListPool.Release(list);
        }

        private void sendFrameStartedToPointers()
        {
            foreach (var pointer in pointers)
                pointer.INTERNAL_FrameStarted();
        }

        private void updatePointers()
        {
            isInsidePointerFrame = true;
            FrameStarted?.InvokeHandleExceptions(this, EventArgs.Empty);

            // need to copy buffers since they might get updated during execution
            List<Pointer> addedList = null;
            List<int> updatedList = null;
            List<int> pressedList = null;
            List<int> releasedList = null;
            List<int> removedList = null;
            List<int> cancelledList = null;

            {
                if (pointersAdded.Count > 0)
                {
                    addedList = pointerListPool.Get();
                    addedList.AddRange(pointersAdded);
                    pointersAdded.Clear();
                }
                if (pointersUpdated.Count > 0)
                {
                    updatedList = intListPool.Get();
                    updatedList.AddRange(pointersUpdated);
                    pointersUpdated.Clear();
                }
                if (pointersPressed.Count > 0)
                {
                    pressedList = intListPool.Get();
                    pressedList.AddRange(pointersPressed);
                    pointersPressed.Clear();
                }
                if (pointersReleased.Count > 0)
                {
                    releasedList = intListPool.Get();
                    releasedList.AddRange(pointersReleased);
                    pointersReleased.Clear();
                }
                if (pointersRemoved.Count > 0)
                {
                    removedList = intListPool.Get();
                    removedList.AddRange(pointersRemoved);
                    pointersRemoved.Clear();
                }
                if (pointersCancelled.Count > 0)
                {
                    cancelledList = intListPool.Get();
                    cancelledList.AddRange(pointersCancelled);
                    pointersCancelled.Clear();
                }
            }

            var count = pointers.Count;
            for (var i = 0; i < count; i++)
            {
                pointers[i].INTERNAL_UpdatePosition();
            }

            if (addedList != null)
            {
                updateAdded(addedList);
                pointerListPool.Release(addedList);
            }

            if (updatedList != null)
            {
                updateUpdated(updatedList);
                intListPool.Release(updatedList);
            }
            if (pressedList != null)
            {
                updatePressed(pressedList);
                intListPool.Release(pressedList);
            }
            if (releasedList != null)
            {
                updateReleased(releasedList);
                intListPool.Release(releasedList);
            }
            if (removedList != null)
            {
                updateRemoved(removedList);
                intListPool.Release(removedList);
            }
            if (cancelledList != null)
            {
                updateCancelled(cancelledList);
                intListPool.Release(cancelledList);
            }

            FrameFinished?.InvokeHandleExceptions(this, EventArgs.Empty);
            isInsidePointerFrame = false;
        }

        private bool wasPointerAddedThisFrame(int id)
        {
            foreach (var p in pointersAdded)
            {
                if (p.Id == id)
                    return true;
            }
            return false;
        }

        #endregion
    }
}