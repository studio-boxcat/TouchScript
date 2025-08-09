/*
 * @author DenizPiri / denizpiri@hotmail.com
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using UnityEngine;

namespace TouchScript
{
    /// <summary>
    /// Extension methods for event handling.
    /// </summary>
    public static class ActionExtensions
    {
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

        public static Exception InvokeHandleExceptions<T0, T1>(this Action<T0, T1> action, T0 arg0, T1 arg1)
        {
            try
            {
                action(arg0, arg1);
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