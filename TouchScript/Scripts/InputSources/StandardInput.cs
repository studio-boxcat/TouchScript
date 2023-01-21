/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using TouchScript.InputSources.InputHandlers;

namespace TouchScript.InputSources
{
    /// <summary>
    /// Processes standard input events (mouse, pointer, pen) on all platforms.
    /// Initializes proper inputs automatically. Replaces old Mobile and Mouse inputs.
    /// </summary>
    public readonly struct StandardInput
    {
        public readonly TouchInputSource TouchInputSource;
        public readonly MouseInputSource MouseInputSource;
        public readonly FakeInputSource FakeInputSource;

        public StandardInput(PointerContainer pointerContainer)
        {
            TouchInputSource = new TouchInputSource(pointerContainer);
            MouseInputSource = new MouseInputSource(pointerContainer);
            FakeInputSource = new FakeInputSource(pointerContainer);
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