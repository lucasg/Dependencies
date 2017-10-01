#if NET40
using System.Collections;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Dragablz.Core
{
    internal static class Extensions
    {
        public static IEnumerable<TContainer> Containers<TContainer>(this ItemsControl itemsControl) where TContainer : class
        {
#if NET45
            for (var i = 0; i < itemsControl.ItemContainerGenerator.Items.Count; i++)
#endif
#if NET40
            var fieldInfo = typeof(ItemContainerGenerator).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance);
            var list = (IList)fieldInfo.GetValue(itemsControl.ItemContainerGenerator);            
            for (var i = 0; i < list.Count; i++)
#endif
            {
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as TContainer;
                if (container != null)
                    yield return container;
            }            
        }

        public static IEnumerable<TObject> Except<TObject>(this IEnumerable<TObject> first, params TObject[] second)
        {
            return first.Except((IEnumerable<TObject>)second);
        }

        public static IEnumerable<object> LogicalTreeDepthFirstTraversal(this DependencyObject node)
        {
            if (node == null) yield break;
            yield return node;

            foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>()
                .SelectMany(depObj => depObj.LogicalTreeDepthFirstTraversal()))            
                yield return child;
        }

        public static IEnumerable<object> VisualTreeDepthFirstTraversal(this DependencyObject node)
        {
            if (node == null) yield break;
            yield return node;

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            {
                var child = VisualTreeHelper.GetChild(node, i);
                foreach (var d in child.VisualTreeDepthFirstTraversal())
                {
                    yield return d;
                }
            }
        }

        /// <summary>
        /// Yields the visual ancestory (including the starting point).
        /// </summary>
        /// <param name="dependencyObject"></param>
        /// <returns></returns>
        public static IEnumerable<DependencyObject> VisualTreeAncestory(this DependencyObject dependencyObject)
        {
            if (dependencyObject == null) throw new ArgumentNullException("dependencyObject");

            while (dependencyObject != null)
            {
                yield return dependencyObject;
                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
            }            
        }

        /// <summary>
        /// Yields the logical ancestory (including the starting point).
        /// </summary>
        /// <param name="dependencyObject"></param>
        /// <returns></returns>
        public static IEnumerable<DependencyObject> LogicalTreeAncestory(this DependencyObject dependencyObject)
        {
            if (dependencyObject == null) throw new ArgumentNullException("dependencyObject");

            while (dependencyObject != null)
            {
                yield return dependencyObject;
                dependencyObject = LogicalTreeHelper.GetParent(dependencyObject);
            }
        }
        
        /// <summary>
        /// Returns the actual Left of the Window independently from the WindowState
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        public static double GetActualLeft(this Window window)
        {
            if (window.WindowState == WindowState.Maximized)
            {
                var leftField = typeof(Window).GetField("_actualLeft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return leftField?.GetValue(window) as double? ?? 0;
            }

            return window.Left;
        }

        /// <summary>
        /// Returns the actual Top of the Window independently from the WindowState
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        public static double GetActualTop(this Window window)
        {
            if (window.WindowState == WindowState.Maximized)
            {
                var topField = typeof(Window).GetField("_actualTop", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return topField?.GetValue(window) as double? ?? 0;
            }

            return window.Top;
        }

    }
}
