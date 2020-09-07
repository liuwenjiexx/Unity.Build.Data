using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityEditor.Build.Data
{ 
    internal static class InternalExtensions
    {
        public static void ClearFileAttributes(this string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                File.SetAttributes(path, FileAttributes.Normal);
        }

        public static int IndexOf(this IEnumerable<string> array,string value, StringComparison comparison)
        {
            if (array == null)
                return -1;
            int index = -1;
            int n = 0;
            foreach(var item in array)
            {
                if (string.Equals(item, value, comparison)) {
                    index = n;
                    break;
                }
                n++;
            }
            return index;
        }
   
    }
}
