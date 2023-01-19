namespace TouchScript.Gestures
{
    /// <summary>
    /// Possible states of a gesture.
    /// </summary>
    public enum GestureState
    {
        /// <summary>
        /// Gesture is idle.
        /// </summary>
        Idle,

        /// <summary>
        /// Gesture started looking for the pattern.
        /// </summary>
        Possible,

        /// <summary>
        /// Continuous gesture has just begun.
        /// </summary>
        Began,

        /// <summary>
        /// Started continuous gesture is updated.
        /// </summary>
        Changed,

        /// <summary>
        /// Continuous gesture is ended.
        /// Gesture is recognized.
        /// </summary>
        Ended,

        /// <summary>
        /// Gesture is cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Gesture is failed by itself or by another recognized gesture.
        /// </summary>
        Failed,
    }

    public static class GestureStateUtils
    {
        public static bool IsIdleOrPossible(this GestureState state)
        {
            return state is GestureState.Idle or GestureState.Possible;
        }

        public static bool IsBeganOrChanged(this GestureState state)
        {
            return state is GestureState.Began or GestureState.Changed;
        }
    }
}