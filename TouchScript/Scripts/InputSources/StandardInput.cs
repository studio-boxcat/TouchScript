/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using JetBrains.Annotations;
using TouchScript.Core;
using TouchScript.InputSources.InputHandlers;
using TouchScript.Pointers;
using UnityEngine;

namespace TouchScript.InputSources
{
    /// <summary>
    /// Processes standard input events (mouse, pointer, pen) on all platforms.
    /// Initializes proper inputs automatically. Replaces old Mobile and Mouse inputs.
    /// </summary>
    [AddComponentMenu("TouchScript/Input Sources/Standard Input")]
    [HelpURL("http://touchscript.github.io/docs/html/T_TouchScript_InputSources_StandardInput.htm")]
    public sealed class StandardInput : InputSource
    {
        #region Private variables

        [NotNull] private MouseHandler mouseHandler;
        [NotNull] private TouchHandler touchHandler;

        #endregion

        #region Public methods

        /// <inheritdoc />
        public override bool UpdateInput()
        {
            var handled = false;

            handled = touchHandler.UpdateInput();

            if (handled) mouseHandler.CancelMousePointer();
            else handled = mouseHandler.UpdateInput();

            return handled;
        }

        /// <inheritdoc />
        public override bool CancelPointer(Pointer pointer, bool shouldReturn)
        {
            var handled = false;
            handled = touchHandler.CancelPointer(pointer, shouldReturn);
            if (!handled) handled = mouseHandler.CancelPointer(pointer, shouldReturn);
            return handled;
        }

        #endregion

        #region Internal methods

        /// <inheritdoc />
        public override void INTERNAL_DiscardPointer(Pointer pointer)
        {
            var handled = false;
            handled = touchHandler.DiscardPointer(pointer);
            if (!handled) mouseHandler.DiscardPointer(pointer);
        }

        #endregion

        #region Private functions

        void Awake()
        {
            Input.simulateMouseWithTouches = false;

            mouseHandler = new MouseHandler(this, TouchManager.Instance);
            touchHandler = new TouchHandler(this, TouchManager.Instance);
        }

        #endregion
    }
}