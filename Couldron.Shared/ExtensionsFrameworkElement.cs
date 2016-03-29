﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Couldron
{
    /// <summary>
    /// Provides usefull extension methods extending <see cref="FrameworkElement"/>
    /// </summary>
    public static class ExtensionsFrameworkElement
    {
        public static void Content<T, TContent>(this T element, Action<TContent> action)
            where T : ContentPresenter
            where TContent : FrameworkElement
        {
            var content = element.Content as TContent;
            if (content != null)
                action(content);
        }

        public static FrameworkElement FindElementWithName(this DependencyObject element, string name)
        {
            if (element == null)
                return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;

                if (child != null && child.Name == name)
                    return child;
            }

            var parent = VisualTreeHelper.GetParent(element);

            if (parent == null)
                return null;

            return FindElementWithName(parent, name);
        }

        public static DependencyObject FindParent(this DependencyObject element, Type dependencyObjectType)
        {
            if (element == null)
                return null;

            var parent = VisualTreeHelper.GetParent(element);

            if (parent == null)
                return null;
            if (parent.GetType() == dependencyObjectType)
                return parent;
            else
                return FindParent(parent, dependencyObjectType);
        }

        public static T FindParent<T>(this DependencyObject element) where T : DependencyObject
        {
            if (element == null)
                return null;

            var parent = VisualTreeHelper.GetParent(element);
            var p = parent as T;

            if (parent != null && p != null)
                return p;
            else
                return FindParent<T>(parent);
        }

        /// <summary>
        /// Returns all visual childs and sub child (recursively) of the element that matches the given type
        /// </summary>
        /// <typeparam name="T">The typ of child to search for</typeparam>
        /// <param name="element">The parent element</param>
        /// <returns>A collection of <see cref="FrameworkElement"/></returns>
        public static IEnumerable<FrameworkElement> FindVisualChildren<T>(this DependencyObject element)
        {
            List<FrameworkElement> elements = new List<FrameworkElement>();
            GetVisualChildren<T>(element as FrameworkElement, elements);
            return elements;
        }

        /// <summary>
        /// Gets the direct visual children of the element
        /// </summary>
        /// <param name="element">The parent element</param>
        /// <returns>A collection of children</returns>
        public static IEnumerable<DependencyObject> GetVisualChildren(this DependencyObject element)
        {
            var result = new List<DependencyObject>();

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
                result.Add(VisualTreeHelper.GetChild(element, i));

            return result;
        }

        /// <summary>
        /// Attaches a binding to this element, based on the provided source property name as a path qualification to the data source.
        /// </summary>
        /// <param name="frameworkElement">The <see cref="FrameworkElement"/> that is extended</param>
        /// <param name="dp">Identifies the destination property where the binding should be established</param>
        /// <param name="source">The object to use as the binding source.</param>
        /// <param name="propertyPath">The path to the binding source property.</param>
        public static void SetBinding(this FrameworkElement frameworkElement, DependencyProperty dp, object source, string propertyPath)
        {
            Binding binding = new Binding();
            binding.Mode = BindingMode.OneWay;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            binding.Source = source;
            binding.Path = new PropertyPath(propertyPath);

            frameworkElement.SetBinding(dp, binding);
        }

        /// <summary>
        /// Attaches a binding to this element, based on the provided source property name as a path qualification to the data source.
        /// </summary>
        /// <param name="frameworkElement">The <see cref="FrameworkElement"/> that is extended</param>
        /// <param name="dp">Identifies the destination property where the binding should be established</param>
        /// <param name="source">The object to use as the binding source.</param>
        /// <param name="propertyPath">The path to the binding source property.</param>
        /// <param name="bindingMode">A value that indicates the direction of the data flow in the binding.</param>
        public static void SetBinding(this FrameworkElement frameworkElement, DependencyProperty dp, object source, string propertyPath, BindingMode bindingMode)
        {
            Binding binding = new Binding();
            binding.Mode = bindingMode;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            binding.Source = source;
            binding.Path = new PropertyPath(propertyPath);

            frameworkElement.SetBinding(dp, binding);
        }

        /// <summary>
        /// Attaches a binding to this element, based on the provided source property name as a path qualification to the data source.
        /// </summary>
        /// <param name="frameworkElement">The <see cref="FrameworkElement"/> that is extended</param>
        /// <param name="dp">Identifies the destination property where the binding should be established</param>
        /// <param name="source">The object to use as the binding source.</param>
        /// <param name="propertyPath">The path to the binding source property.</param>
        /// <param name="bindingMode">A value that indicates the direction of the data flow in the binding.</param>
        /// <param name="valueConverterName">The converter name to use</param>
        public static void SetBinding(this FrameworkElement frameworkElement, DependencyProperty dp, object source, string propertyPath, BindingMode bindingMode, string valueConverterName)
        {
            Binding binding = new Binding();
            binding.Mode = bindingMode;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            binding.Source = source;
            binding.Path = new PropertyPath(propertyPath);
            binding.Converter = Application.Current.Resources[valueConverterName] as IValueConverter;

            frameworkElement.SetBinding(dp, binding);
        }

        /// <summary>
        /// Attaches a binding to this element, based on the provided source property name as a path qualification to the data source.
        /// </summary>
        /// <param name="frameworkElement">The <see cref="FrameworkElement"/> that is extended</param>
        /// <param name="dp">Identifies the destination property where the binding should be established</param>
        /// <param name="source">The object to use as the binding source.</param>
        /// <param name="propertyPath">The path to the binding source property.</param>
        /// <param name="bindingMode">A value that indicates the direction of the data flow in the binding.</param>
        /// <param name="valueConverter">The converter to use</param>
        /// <param name="converterParameter">the parameter to pass to the Converter.</param>
        public static void SetBinding(this FrameworkElement frameworkElement, DependencyProperty dp, object source, string propertyPath, BindingMode bindingMode, IValueConverter valueConverter, object converterParameter)
        {
            Binding binding = new Binding();
            binding.Mode = bindingMode;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            binding.Source = source;
            binding.Path = new PropertyPath(propertyPath);
            binding.Converter = valueConverter;
            binding.ConverterParameter = converterParameter;

            frameworkElement.SetBinding(dp, binding);
        }

        private static void GetVisualChildren<T>(FrameworkElement element, List<FrameworkElement> list)
        {
            if (element != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
                {
                    var child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;

                    if (child != null && child is T)
                        list.Add(child);

                    if (child != null)
                        GetVisualChildren<T>(child, list);
                }
            }
        }
    }
}