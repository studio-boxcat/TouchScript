/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using TouchScript.Pointers;

namespace TouchScript.InputSources
{
    /// <summary>
    /// An object which represents an input source.
    /// </summary>
    /// <remarks>
    /// <para>In TouchScript all pointer points (<see cref="Pointer"/>) come from input sources.</para>
    /// <para>If you want to feed pointers to the library the best way to do it is to create a custom input source.</para>
    /// </remarks>
    public interface IInputSource : IInputHandler, INTERNAL_IInputSource
    {
    }

    /// <summary>
    /// Internal methods for <see cref="IInputSource"/>. DO NOT CALL THESE METHODS DIRECTLY FROM YOUR CODE!
    /// </summary>
    public interface INTERNAL_IInputSource
    {
        /// <summary>
        /// Used by <see cref="TouchManagerInstance"/> to return a pointer to input source.
        /// DO NOT CALL THIS METHOD DIRECTLY FROM YOUR CODE!
        /// </summary>
        /// <param name="pointer">The pointer.</param>
        void INTERNAL_DiscardPointer(Pointer pointer);

        /// <summary>
        /// Used by <see cref="TouchManagerInstance"/> to return sync resolution on inputs.
        /// DO NOT CALL THIS METHOD DIRECTLY FROM YOUR CODE!
        /// </summary>
        void INTERNAL_UpdateResolution();
    }

    public interface IInputHandler
    {
        /// <summary>
        /// This method is called by <see cref="ITouchManager"/> to synchronously update the input.
        /// </summary>
        /// <returns><c>True</c> if the input source was updated; <c>false</c> otherwise.</returns>
        bool UpdateInput();

        /// <summary>
        /// Cancels the pointer.
        /// </summary>
        /// <param name="pointer">The pointer.</param>
        /// <param name="shouldReturn">if set to <c>true</c> returns the pointer back to the system with different id.</param>
        /// <returns><c>True</c> if the pointer belongs to this Input and was successfully cancelled; <c>false</c> otherwise.</returns>
        bool CancelPointer(Pointer pointer, bool shouldReturn);
    }

}