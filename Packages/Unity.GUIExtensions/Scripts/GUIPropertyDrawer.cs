using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEngine.GUIExtensions
{

    public abstract class GUIPropertyDrawer
    {
        static Dictionary<Type, Cached> drawers;

        class Cached
        {
            /// <summary>
            /// ConfigurationPropertyAttribute or Value Type
            /// </summary>
            public Type targetType;
            public int priority;
            public bool useForChildren;
            public Type drawerType;
            public GUIPropertyDrawer drawer;
        }

        public abstract void OnGUILayout(IGUIProperty property, Attribute attribute);

        private static void CacheDrawerTypes()
        {
            if (drawers == null)
            {
                Cached cached;

                drawers = new Dictionary<Type, Cached>();

                foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
                    .Referenced(typeof(GUIPropertyDrawer).Assembly)
                    .SelectMany(o => o.GetTypes()))
                {
                    if (type.IsAbstract)
                        continue;
                    if (!typeof(GUIPropertyDrawer).IsAssignableFrom(type))
                        continue;

                    var customAttr = (CustomGUIPropertyDrawerAttribute)type.GetCustomAttributes(typeof(CustomGUIPropertyDrawerAttribute), true).FirstOrDefault();
                    if (customAttr != null)
                    {
                        drawers.TryGetValue(customAttr.TargetType, out cached);
                        if (cached == null || cached.priority < customAttr.Priority)
                        {
                            if (cached == null)
                            {
                                cached = new Cached();
                                drawers[customAttr.TargetType] = cached;
                            }
                            
                            cached.targetType = customAttr.TargetType;
                            cached.priority = customAttr.Priority;
                            cached.useForChildren = customAttr.UseForChildren;
                            cached.drawerType = type;
                        }
                    }
                }
            }
        }

        public static GUIPropertyDrawer GetDrawer(Type targetType)
        {
            CacheDrawerTypes();

            Cached cached;

            Type type = targetType;
            while (type != null)
            {
                if (drawers.TryGetValue(type, out cached))
                {
                    if (cached.targetType == targetType || cached.useForChildren)
                    {
                        if (cached.drawer == null)
                            cached.drawer = (GUIPropertyDrawer)Activator.CreateInstance(cached.drawerType);
                        return cached.drawer;
                    }
                }
                type = type.BaseType;
            }
            return null;
        }

    }




}
