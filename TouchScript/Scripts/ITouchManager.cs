/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Collections.Generic;
using TouchScript.Devices.Display;
using TouchScript.Pointers;

namespace TouchScript
{
    /// <summary>
    /// Arguments dispatched with TouchManager events.
    /// </summary>
    public class PointerEventArgs : EventArgs
    {
        /// <summary>
        /// Gets list of pointers participating in the event.
        /// </summary>
        /// <value>List of pointers added, changed or removed this frame.</value>
        public IList<Pointer> Pointers { get; private set; }

        private static PointerEventArgs instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="PointerEventArgs"/> class.
        /// </summary>
        private PointerEventArgs() {}

        /// <summary>
        /// Returns cached instance of EventArgs.
        /// This cached EventArgs is reused throughout the library not to alocate new ones on every call.
        /// </summary>
        /// <param name="pointers">A list of pointers for event.</param>
        /// <returns>Cached EventArgs object.</returns>
        public static PointerEventArgs GetCachedEventArgs(IList<Pointer> pointers)
        {
            if (instance == null) instance = new PointerEventArgs();
            instance.Pointers = pointers;
            return instance;
        }
    }
}