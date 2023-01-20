/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using TouchScript.InputSources.InputHandlers;
using TouchScript.Pointers;

namespace TouchScript.InputSources
{
    /// <summary>
    /// Processes standard input events (mouse, pointer, pen) on all platforms.
    /// Initializes proper inputs automatically. Replaces old Mobile and Mouse inputs.
    /// </summary>
    public sealed class StandardInput
    {
        readonly PointerContainer _pointerContainer;
        public readonly TouchInputSource TouchInputSource;
        public readonly MouseInputSource MouseInputSource;
        public readonly FakeInputSource FakeInputSource;

        public StandardInput()
        {
            _pointerContainer = new PointerContainer(4);
            TouchInputSource = new TouchInputSource(_pointerContainer);
            MouseInputSource = new MouseInputSource(_pointerContainer);
            FakeInputSource = new FakeInputSource(_pointerContainer);
        }

        public List<Pointer> GetPointers()
        {
            return _pointerContainer.Pointers;
        }

        public void UpdateInput(PointerChanges changes)
        {
            var handled = TouchInputSource.UpdateInput(changes);

            if (handled) MouseInputSource.CancelMousePointer(changes);
            else MouseInputSource.UpdateInput(changes);

            FakeInputSource.UpdateInput(changes);
        }

        public void CancelAllPointers(PointerChanges changes)
        {
            TouchInputSource.CancelAllPointers(changes);
            MouseInputSource.CancelAllPointers(changes);
            FakeInputSource.CancelAllPointers(changes);
        }
    }
}