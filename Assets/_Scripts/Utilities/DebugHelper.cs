using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Game.Utils
{
    public static class DebugHelper
    {
         public static void ClearLog()
        {
            var assembly = Assembly.GetAssembly(typeof(UnityEditor.ActiveEditorTracker));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(null, null);
        }
         
    }
}