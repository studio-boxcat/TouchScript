/*
 * @author DenizPiri / denizpiri@hotmail.com
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using UnityEngine;

namespace TouchScript.Utils
{
    /// <summary>
    /// Extension methods for event handling.
    /// </summary>
    public static class ActionExtensions
    {
        /// <summary>
        /// Invokes an event handling exceptions.
        /// </summary>
        /// <typeparam name="T"> EventArgs type. </typeparam>
        /// <param name="action"> Event. </param>
        /// <param name="arg"> EventArgs. </param>
        /// <returns> The exception caught or <c>null</c>. </returns>
        public static Exception InvokeHandleExceptions<T>(this Action<T> action, T arg)
        {
            try
            {
                action(arg);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Invokes an event handling exceptions.
        /// </summary>
        /// <param name="action"> Event. </param>
        /// <param name="sender"> Event sender. </param>
        /// <param name="args"> EventArgs. </param>
        /// <returns> The exception caught or <c>null</c>. </returns>
        public static Exception InvokeHandleExceptions(this Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return ex;
            }
            return null;
        }
    }
}