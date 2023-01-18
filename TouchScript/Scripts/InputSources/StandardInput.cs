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
    public sealed class StandardInput : MonoBehaviour
    {
        #region Private variables

        [NotNull] private MouseSource _mouseSource;
        [NotNull] private TouchSource _touchSource;

        #endregion

        #region Public methods

        /// <inheritdoc />
        public bool UpdateInput()
        {
            var handled = false;

            handled = _touchSource.UpdateInput();

            if (handled) _mouseSource.CancelMousePointer();
            else handled = _mouseSource.UpdateInput();

            return handled;
        }

        #endregion

        #region Private functions

        void Awake()
        {
            Input.simulateMouseWithTouches = false;

            _mouseSource = new MouseSource(TouchManager.Instance);
            _touchSource = new TouchSource(TouchManager.Instance);
        }

        #endregion
    }
}