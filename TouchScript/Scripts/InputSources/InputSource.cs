/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using JetBrains.Annotations;
using TouchScript.Core;
using TouchScript.Pointers;
using UnityEngine;

namespace TouchScript.InputSources
{
    /// <summary>
    /// Base class for all pointer input sources.
    /// </summary>
    public abstract class InputSource : MonoBehaviour, IInputSource
    {
        #region Private variables

        private TouchManagerInstance touchManager;

        #endregion

        #region Public methods

        /// <inheritdoc />
        public virtual bool UpdateInput()
        {
            return false;
        }

        /// <inheritdoc />
        public virtual bool CancelPointer(Pointer pointer, bool shouldReturn)
        {
            return false;
        }

        #endregion

        #region Internal methods

        /// <inheritdoc />
        public virtual void INTERNAL_DiscardPointer([NotNull] Pointer pointer) {}

        #endregion

        #region Unity methods

        /// <summary>
        /// Unity OnEnable callback.
        /// </summary>
        private void OnEnable()
        {
            touchManager = TouchManagerInstance.Instance;
            if (touchManager == null) throw new InvalidOperationException("TouchManager instance is required!");
            touchManager.AddInput(this);

            init();
        }

        /// <summary>
        /// Unity OnDestroy callback.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (touchManager != null)
            {
                touchManager.RemoveInput(this);
                touchManager = null;
            }
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Initializes the input source.
        /// </summary>
        protected virtual void init() {}

        #endregion
    }
}