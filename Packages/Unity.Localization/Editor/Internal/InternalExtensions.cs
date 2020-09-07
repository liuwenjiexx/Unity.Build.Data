using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using UnityEngine;


namespace UnityEditor.Localizations
{
    public static class InternalExtensions
    {
        public static string ReplacePathSeparator(this string path)
        {
            if (path == null)
                return null;
            if (Path.DirectorySeparatorChar == '/')
                path = path.Replace('\\', Path.DirectorySeparatorChar);
            else
                path = path.Replace('/', Path.DirectorySeparatorChar);
            return path;
        }
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

        public static IEnumerable<Assembly> Referenced(this IEnumerable<Assembly> assemblies, Assembly referenced)
        {
            string fullName = referenced.FullName;

            foreach (var ass in assemblies)
            {
                if (referenced == ass)
                {
                    yield return ass;
                }
                else
                {
                    foreach (var refAss in ass.GetReferencedAssemblies())
                    {
                        if (fullName == refAss.FullName)
                        {
                            yield return ass;
                            break;
                        }
                    }
                }
            }
        }

        #region Xml

        public static bool HasAttribute(this XmlNode node, string name)
        {
            if (node.Attributes == null)
                return false;
            var attr = node.Attributes[name];
            if (attr == null)
                return false;
            return true;
        }
        public static XmlAttribute GetOrAddAttribute(this XmlNode node, string name, string defaultValue)
        {
            var attr = node.Attributes[name];
            if (attr == null)
            {
                attr = node.OwnerDocument.CreateAttribute(name);
                attr.Value = defaultValue;
            }
            return attr;
        }

        public static void SetOrAddAttributeValue(this XmlNode node, string name, string value)
        {
            if (node.Attributes == null)
                return;
            var attr = node.Attributes[name];
            if (attr == null)
            {
                attr = node.OwnerDocument.CreateAttribute(name);
                node.Attributes.Append(attr);
            }

            attr.Value = value;
        }

        public static bool TryGetAttributeValue(this XmlNode node, string name, out string value)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (node.Attributes == null)
            {
                value = null;
                return false;
            }
            var attr = node.Attributes[name];
            if (attr == null)
            {
                value = null;
                return false;
            }
            value = attr.Value;
            return true;
        }
        public static bool TryGetAttributeValue<T>(this XmlNode node, string name, out T value)
        {
            string str;
            if (node.TryGetAttributeValue(name, out str))
            {
                Type type = typeof(T);
                if (type.IsEnum)
                {
                    value = (T)Enum.Parse(type, str);
                }
                else
                {
                    value = (T)Convert.ChangeType(str, type);
                }
                return true;
            }
            value = default(T);
            return false;
        }

        public static T GetAttributeValue<T>(this XmlNode node, string name, T defaultValue)
        {
            T value;
            if (!TryGetAttributeValue(node, name, out value))
            {
                value = defaultValue;
            }
            return value;
        }


        public static string GetOrAddAttributeValue(this XmlNode node, string name, string defaultValue)
        {
            var attr = GetOrAddAttribute(node, name, defaultValue);
            return attr.Value;
        }

        public static T GetOrAddAttributeValue<T>(this XmlNode node, string name, T defaultValue)
        {
            T value;
            if (!TryGetAttributeValue<T>(node, name, out value))
            {
                SetOrAddAttributeValue(node, name, defaultValue != null ? defaultValue.ToString() : string.Empty);
                value = defaultValue;
            }
            return value;
        }



        public static void RemoveAttribute(this XmlNode node, string name)
        {
            var attr = node.Attributes[name];
            if (attr != null)
            {
                node.Attributes.Remove(attr);
            }
        }

        #endregion
    }
}