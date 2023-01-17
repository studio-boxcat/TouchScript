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
        /// Invalid pointer id.
        /// </summary>
        public const int INVALID_POINTER = -1;

        /// <summary>
        /// This pointer is generated by script and is not mapped to any device input.
        /// </summary>
        public const uint FLAG_ARTIFICIAL = 1 << 0;

        /// <summary>
        /// This pointer was returned to the system after it was cancelled.
        /// </summary>
        public const uint FLAG_RETURNED = 1 << 1;

        /// <summary>
        /// This pointer is internal and shouldn't be shown on screen.
        /// </summary>
        public const uint FLAG_INTERNAL = 1 << 2;

        /// <summary>
        /// The state of buttons for a pointer. Combines 3 types of button events: Pressed (holding a button), Down (just pressed this frame) and Up (released this frame).
        /// </summary>
        [Flags]
        public enum PointerButtonState
        {
            /// <summary>
            /// No button is pressed.
            /// </summary>
            Nothing = 0,

            /// <summary>
            /// Indicates a primary action, analogous to a left mouse button down.
            /// A <see cref="TouchPointer"/> or <see cref="ObjectPointer"/> has this flag set when it is in contact with the digitizer surface.
            /// A <see cref="PenPointer"/> has this flag set when it is in contact with the digitizer surface with no buttons pressed.
            /// A <see cref="MousePointer"/> has this flag set when the left mouse button is down.
            /// </summary>
            FirstButtonPressed = 1 << 0,

            /// <summary>
            /// First button pressed this frame.
            /// </summary>
            FirstButtonDown = 1 << 11,

            /// <summary>
            /// First button released this frame.
            /// </summary>
            FirstButtonUp = 1 << 12,

            /// <summary>
            /// Any button is pressed.
            /// </summary>
            AnyButtonPressed = FirstButtonPressed,

            /// <summary>
            /// Any button down this frame.
            /// </summary>
            AnyButtonDown = FirstButtonDown,

            /// <summary>
            /// Any button up this frame.
            /// </summary>
            AnyButtonUp = FirstButtonUp
        }

        #endregion

        #region Public properties

        /// <inheritdoc />
        public int Id { get; private set; }

        /// <inheritdoc />
        public PointerButtonState Buttons { get; set; }

        [NotNull]
        public readonly IInputSource InputSource;

        /// <inheritdoc />
        public Vector2 Position
        {
            get { return position; }
            set { newPosition = value; }
        }

        /// <inheritdoc />
        public Vector2 PreviousPosition { get; private set; }

        public Vector2 ScrollDelta { get; set; }

        /// <inheritdoc />
        public uint Flags { get; set; }

        #endregion

        #region Private variables

        private static StringBuilder builder;

        private int refCount = 0;
        private Vector2 position, newPosition;
        private HitData pressData, overData;
        private bool overDataIsDirty = true;

        #endregion

        #region Public methods

        /// <inheritdoc />
        public HitData GetOverData(bool forceRecalculate = false)
        {
            if (overDataIsDirty || forceRecalculate)
            {
                LayerManager.GetHitTarget(position, out overData);
                overDataIsDirty = false;
            }
            return overData;
        }

        /// <summary>
        /// Returns <see cref="HitData"/> when the pointer was pressed. If the pointer is not pressed uninitialized <see cref="HitData"/> is returned.
        /// </summary>
        public HitData GetPressData()
        {
            return pressData;
        }

        /// <summary>
        /// Copies values from the target.
        /// </summary>
        /// <param name="target">The target pointer to copy values from.</param>
        public void CopyFrom(Pointer target)
        {
            Flags = target.Flags;
            Buttons = target.Buttons;
            position = target.position;
            newPosition = target.newPosition;
            PreviousPosition = target.PreviousPosition;
            ScrollDelta = target.ScrollDelta;
        }

        /// <inheritdoc />
        public override bool Equals(object other)
        {
            return Equals(other as Pointer);
        }

        /// <inheritdoc />
        public bool Equals(Pointer other)
        {
            if (other == null)
                return false;

            return Id == other.Id;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Id;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (builder == null) builder = new StringBuilder();
            builder.Length = 0;
            builder.Append("(Pointer id: ");
            builder.Append(Id);
            builder.Append(", buttons: ");
            PointerUtils.PressedButtonsToString(Buttons, builder);
            builder.Append(", flags: ");
            BinaryUtils.ToBinaryString(Flags, builder, 8);
            builder.Append(", position: ");
            builder.Append(Position);
            builder.Append(")");
            return builder.ToString();
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

        internal void INTERNAL_Init(int id)
        {
            Id = id;
            PreviousPosition = position = newPosition;
        }

        internal void INTERNAL_Reset()
        {
            Id = INVALID_POINTER;
            INTERNAL_ClearPressData();
            position = newPosition = PreviousPosition = Vector2.zero;
            Flags = 0;
            Buttons = PointerButtonState.Nothing;
            overDataIsDirty = true;
        }

        internal void INTERNAL_FrameStarted()
        {
            Buttons &= ~(PointerButtonState.AnyButtonDown | PointerButtonState.AnyButtonUp);
            overDataIsDirty = true;
        }

        internal void INTERNAL_UpdatePosition()
        {
            PreviousPosition = position;
            position = newPosition;
        }

        internal void INTERNAL_Retain()
        {
            refCount++;
        }

        internal int INTERNAL_Release()
        {
            return --refCount;
        }

        internal void INTERNAL_SetPressData(HitData data)
        {
            pressData = data;
            overData = data;
            overDataIsDirty = false;
        }

        internal void INTERNAL_ClearPressData()
        {
            pressData = default;
            refCount = 0;
        }

        #endregion
    }
}