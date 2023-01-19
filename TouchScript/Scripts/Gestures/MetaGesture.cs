/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Collections.Generic;
using TouchScript.Pointers;
using TouchScript.Utils;
using UnityEngine;

namespace TouchScript.Gestures
{
    /// <summary>
    /// Converts Pointer events for target object into separate events to be used somewhere else.
    /// </summary>
    [AddComponentMenu("TouchScript/Gestures/Meta Gesture")]
    [HelpURL("http://touchscript.github.io/docs/html/T_TouchScript_Gestures_MetaGesture.htm")]
    public sealed class MetaGesture : Gesture
    {
        #region Events

        /// <summary>
        /// Occurs when a pointer is added.
        /// </summary>
        public event Action<Pointer> PointerPressed;

        /// <summary>
        /// Occurs when a pointer is updated.
        /// </summary>
        public event Action<Pointer> PointerUpdated;

        /// <summary>
        /// Occurs when a pointer is removed.
        /// </summary>
        public event Action<Pointer> PointerReleased;

        /// <summary>
        /// Occurs when a pointer is cancelled.
        /// </summary>
        public event Action<Pointer> PointerCancelled;

		#endregion

		#region Gesture callbacks

		/// <inheritdoc />
		protected override void pointersPressed(IList<Pointer> pointers)
        {
            base.pointersPressed(pointers);

            if (State == GestureState.Idle) setState(GestureState.Began);

            var length = pointers.Count;
            if (PointerPressed != null)
            {
                for (var i = 0; i < length; i++)
                    PointerPressed.InvokeHandleExceptions(pointers[i]);
            }
        }

        /// <inheritdoc />
        protected override void pointersUpdated(IList<Pointer> pointers)
        {
            base.pointersUpdated(pointers);

            if (State.IsBeganOrChanged()) setState(GestureState.Changed);

            var length = pointers.Count;
            if (PointerUpdated != null)
            {
                for (var i = 0; i < length; i++)
                    PointerUpdated.InvokeHandleExceptions(pointers[i]);
            }
        }

        /// <inheritdoc />
        protected override void pointersReleased(IList<Pointer> pointers)
        {
            base.pointersReleased(pointers);

            if (State.IsBeganOrChanged() && NumPointers == 0) setState(GestureState.Ended);

            var length = pointers.Count;
            if (PointerReleased != null)
            {
                for (var i = 0; i < length; i++)
                    PointerReleased.InvokeHandleExceptions(pointers[i]);
            }
        }

        /// <inheritdoc />
        protected override void pointersCancelled(IList<Pointer> pointers)
        {
            base.pointersCancelled(pointers);

            var length = pointers.Count;
            if (PointerCancelled != null)
            {
                for (var i = 0; i < length; i++)
                    PointerCancelled.InvokeHandleExceptions(pointers[i]);
            }
        }

        #endregion
    }
}