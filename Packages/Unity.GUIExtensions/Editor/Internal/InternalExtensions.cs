using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityEditor
{
    public static class InternalExtensions
    {

        public static bool ToRelativePath(this string path, string relativeTo, out string result)
        {
            string fullRelativeTo = Path.GetFullPath(relativeTo).Trim();
            string fullPath = Path.GetFullPath(path).Trim();

            if (fullPath.EndsWith("/") || fullPath.EndsWith("\\"))
                fullPath = fullPath.Substring(0, fullPath.Length - 1);
            if (fullRelativeTo.EndsWith("/") || fullRelativeTo.EndsWith("\\"))
                fullRelativeTo = fullRelativeTo.Substring(0, fullRelativeTo.Length - 1);

            string[] relativeToParts = fullRelativeTo.Split('/', '\\');
            string[] fullPathParts = fullPath.Split('/', '\\');
            int index = -1;

            if (fullPathParts.Length <= 1)
            {
                result = path;
                return false;
            }

            if (!string.Equals(fullPathParts[0], relativeToParts[0], StringComparison.InvariantCultureIgnoreCase))
            {
                result = path;
                return false;
            }


            for (int i = 0; i < fullPathParts.Length && i < relativeToParts.Length; i++)
            {
                if (!string.Equals(fullPathParts[i], relativeToParts[i], StringComparison.InvariantCultureIgnoreCase))
                    break;
                index = i;
            }

            result = "";
            for (int i = index + 1; i < relativeToParts.Length; i++)
            {
                if (result.Length > 0)
                    result += Path.DirectorySeparatorChar;
                result += "..";
            }
            for (int i = index + 1; i < fullPathParts.Length; i++)
            {
                if (result.Length > 0)
                    result += Path.DirectorySeparatorChar;
                result += fullPathParts[i];
            }
            return true;
        }
    }


}