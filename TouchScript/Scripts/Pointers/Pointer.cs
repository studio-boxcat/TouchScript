/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using TouchScript.Core;
using TouchScript.Hit;
using TouchScript.InputSources;
using UnityEngine;

namespace TouchScript.Pointers
{
    /// <summary>
    /// <para>Representation of a pointer (touch, mouse) within TouchScript.</para>
    /// <para>An instance of this class is created when user touches the screen. A unique id is assigned to it which doesn't change throughout its life.</para>
    /// <para><b>Attention!</b> Do not store references to these objects beyond pointer's lifetime (i.e. when target finger is lifted off). These objects may be reused internally. Store unique ids instead.</para>
    /// </summary>
    public sealed class Pointer : IEquatable<Pointer>
    {
        #region Public properties

        public PointerId Id { get; private set; }
        public bool Pressing;
        public IInputSource InputSource { get; private set; }
        public Vector2 Position { get; private set; }
        public Vector2 NewPosition;
        public Vector2 PreviousPosition { get; private set; }
        public bool IsReturned;

        #endregion

        #region Private variables

        int _refCount = 0;
        HitData _pressData, _overData;
        bool _overDataIsDirty = true;

        #endregion

        #region Public methods

        public HitData GetOverData(bool forceRecalculate = false)
        {
            if (_overDataIsDirty || forceRecalculate)
            {
                LayerManager.GetHitTarget(Position, out _overData);
                _overDataIsDirty = false;
            }
            return _overData;
        }

        /// <summary>
        /// Returns <see cref="HitData"/> when the pointer was pressed. If the pointer is not pressed uninitialized <see cref="HitData"/> is returned.
        /// </summary>
        public HitData GetPressData()
        {
            return _pressData;
        }

        /// <summary>
        /// Copies values from the target.
        /// </summary>
        /// <param name="target">The target pointer to copy values from.</param>
        public void CopyPositions(Pointer target)
        {
            Position = target.Position;
            NewPosition = target.NewPosition;
            PreviousPosition = target.PreviousPosition;
        }

        public override bool Equals(object other) => Equals(other as Pointer);

        public bool Equals(Pointer other)
        {
            if (other == null) return false;
            return Id == other.Id;
        }

        public override int GetHashCode() => (int) Id;

        public override string ToString() => $"{((int) Id).ToString()} ({InputSource?.GetType().Name ?? "null"})";

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Pointer"/> class.
        /// </summary>
        public Pointer()
        {
            INTERNAL_Reset();
        }

        #endregion

        #region Internal methods

        public void INTERNAL_Init(PointerId id, IInputSource inputSource, Vector2 position)
        {
            Id = id;
            InputSource = inputSource;
            PreviousPosition = Position = NewPosition = position;
        }

        public void INTERNAL_Reset()
        {
            Id = PointerId.Invalid;
            INTERNAL_ClearPressData();
            InputSource = null;
            Position = NewPosition = PreviousPosition = default;
            IsReturned = false;
            Pressing = false;
            _overDataIsDirty = true;
        }

        internal void SetOverDataDirty()
        {
            _overDataIsDirty = true;
        }

        internal void INTERNAL_UpdatePosition()
        {
            PreviousPosition = Position;
            Position = NewPosition;
        }

        internal void INTERNAL_Retain()
        {
            _refCount++;
        }

        internal int INTERNAL_Release()
        {
            return --_refCount;
        }

        internal void INTERNAL_SetPressData(HitData data)
        {
            _pressData = data;
            _overData = data;
            _overDataIsDirty = false;
        }

        internal void INTERNAL_ClearPressData()
        {
            _pressData = default;
            _refCount = 0;
        }

        #endregion
    }
}