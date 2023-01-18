/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Text;
using JetBrains.Annotations;
using TouchScript.Core;
using TouchScript.Hit;
using TouchScript.InputSources;
using TouchScript.Utils;
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
        #region Constants

        /// <summary>
        /// This pointer was returned to the system after it was cancelled.
        /// </summary>
        public const uint FLAG_RETURNED = 1 << 1;

        /// <summary>
        /// The state of buttons for a pointer. Combines 3 types of button events: Pressed (holding a button), Down (just pressed this frame) and Up (released this frame).
        /// </summary>
        [Flags]
        public enum PointerButtonState : byte
        {
            /// <summary>
            /// Any button is pressed.
            /// </summary>
            ButtonPressed = 1 << 0,

            /// <summary>
            /// Any button down this frame.
            /// </summary>
            ButtonDown = 1 << 1,

            /// <summary>
            /// Any button up this frame.
            /// </summary>
            ButtonUp = 1 << 2
        }

        #endregion

        #region Public properties

        /// <inheritdoc />
        public PointerId Id { get; private set; }

        /// <inheritdoc />
        public PointerButtonState Buttons { get; set; }

        [NotNull]
        public readonly IInputSource InputSource;

        /// <inheritdoc />
        public Vector2 Position
        {
            get => _position;
            set => _newPosition = value;
        }

        /// <inheritdoc />
        public Vector2 PreviousPosition { get; private set; }

        public Vector2 ScrollDelta { get; set; }

        /// <inheritdoc />
        public uint Flags { get; set; }

        #endregion

        #region Private variables

        static StringBuilder _sb;

        int _refCount = 0;
        Vector2 _position, _newPosition;
        HitData _pressData, _overData;
        bool _overDataIsDirty = true;

        #endregion

        #region Public methods

        /// <inheritdoc />
        public HitData GetOverData(bool forceRecalculate = false)
        {
            if (_overDataIsDirty || forceRecalculate)
            {
                LayerManager.GetHitTarget(_position, out _overData);
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
        public void CopyFrom(Pointer target)
        {
            Flags = target.Flags;
            Buttons = target.Buttons;
            _position = target._position;
            _newPosition = target._newPosition;
            PreviousPosition = target.PreviousPosition;
            ScrollDelta = target.ScrollDelta;
        }

        /// <inheritdoc />
        public override bool Equals(object other) => Equals(other as Pointer);

        /// <inheritdoc />
        public bool Equals(Pointer other)
        {
            if (other == null)
                return false;

            return Id == other.Id;
        }

        /// <inheritdoc />
        public override int GetHashCode() => (int) Id;

        /// <inheritdoc />
        public override string ToString()
        {
            _sb ??= new StringBuilder();
            _sb.Length = 0;
            _sb.Append("(Pointer id: ");
            _sb.Append(Id);
            _sb.Append(", buttons: ");
            PointerUtils.PressedButtonsToString(Buttons, _sb);
            _sb.Append(", flags: ");
            BinaryUtils.ToBinaryString(Flags, _sb, 8);
            _sb.Append(", position: ");
            _sb.Append(Position);
            _sb.Append(")");
            return _sb.ToString();
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Pointer"/> class.
        /// </summary>
        public Pointer(IInputSource input)
        {
            InputSource = input;
            INTERNAL_Reset();
        }

        #endregion

        #region Internal methods

        public void INTERNAL_Init(PointerId id, Vector2 position)
        {
            Id = id;
            PreviousPosition = _position = _newPosition = position;
        }

        public void INTERNAL_Reset()
        {
            Id = PointerId.Invalid;
            INTERNAL_ClearPressData();
            _position = _newPosition = PreviousPosition = default;
            Flags = 0;
            Buttons = default;
            _overDataIsDirty = true;
        }

        internal void INTERNAL_FrameStarted()
        {
            Buttons &= ~(PointerButtonState.ButtonDown | PointerButtonState.ButtonUp);
            _overDataIsDirty = true;
        }

        internal void INTERNAL_UpdatePosition()
        {
            PreviousPosition = _position;
            _position = _newPosition;
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