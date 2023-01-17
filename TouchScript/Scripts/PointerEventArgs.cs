/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using TouchScript.Pointers;

namespace TouchScript
{
    /// <summary>
    /// Arguments dispatched with TouchManager events.
    /// </summary>
    public struct PointerEventArgs
    {
        public readonly IReadOnlyList<Pointer> Pointers;

        public PointerEventArgs(IReadOnlyList<Pointer> pointers)
        {
            Pointers = pointers;
        }
    }
}