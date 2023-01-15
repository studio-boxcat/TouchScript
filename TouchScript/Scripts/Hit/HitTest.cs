/*
 * @author Valentin Simonov / http://va.lent.in/
 */

namespace TouchScript.Hit
{

    /// <summary>
    /// Result of a check to find if a hit object should recieve this pointer or not.
    /// </summary>
    public enum HitResult
    {
        /// <summary>
        /// This is a hit, object should recieve pointer.
        /// </summary>
        Hit = 1,

        /// <summary>
        /// Object should not recieve pointer.
        /// </summary>
        Miss = 2,

        /// <summary>
        /// Object should not recieve pointer and this pointer should be discarded and not tested with any other object.
        /// </summary>
        Discard = 3
    }
}