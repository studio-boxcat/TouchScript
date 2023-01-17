/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using TouchScript.Pointers;
using UnityEngine;

namespace TouchScript.InputSources
{
    /// <summary>
    /// Base class for all pointer input sources.
    /// </summary>
    public abstract class InputSource : MonoBehaviour, IInputSource
    {
        public abstract bool UpdateInput();
        public abstract bool CancelPointer(Pointer pointer, bool shouldReturn);
        public abstract void INTERNAL_DiscardPointer(Pointer pointer);
    }
}