// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Provides a <see cref="DependencyProperty"/> that can be the target of a binding.
    /// </summary>
    /// <remarks>
    /// This class is useful as an intermediate target for bindings. It can be used indirectly to bind
    /// two properties that are not dependency properties, among other things.
    /// </remarks>
    public class BindableContainer : DependencyObject
    {
        /// <summary>
        /// Identifies the Value dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register ("Value", typeof (object), typeof (BindableContainer));

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public object Value
        {
            get
            {
                return (object)this.GetValue (BindableContainer.ValueProperty);
            }
            set
            {
                this.SetValue (BindableContainer.ValueProperty, value);
            }
        }
    }

    /// <summary>
    /// Provides various Wpf and Xaml extensions.
    /// </summary>
    public static class Extensions
    {
        #region Dependency Properties
        /// <summary>
        /// Identifies the EventBindings attached property.
        /// </summary>
        /// <example>
        /// <![CDATA[
        ///   <ListBox>
        ///     <ev:Extensions.EventBindings>
        ///       <ev:EventBindingCollection>
        ///         <ev:EventBinding RoutedEvent="Selector.SelectionChanged" Command="ApplicationCommands.Save" CommandParameter="{Binding RelativeSource={RelativeSource Self},Path=Source.SelectedItem.Content}" />
        ///       </ev:EventBindingCollection>
        ///     </ev:Extensions.EventBindings>
        ///    <ListBoxItem>c:\file1</ListBoxItem>
        ///    <ListBoxItem>c:\file2</ListBoxItem>
        ///   </ListBox>
        /// ]]>
        /// </example>
        public static readonly DependencyProperty EventBindingsProperty =
            DependencyProperty.RegisterAttached ("EventBindings", typeof (EventBindingCollection), typeof (Extensions), new PropertyMetadata (Extensions.OnEventBindingsChanged));

        /// <summary>
        /// Identifies the GroupStyles attached property.
        /// </summary>
        /// <example>
        /// <![CDATA[
        ///   <ListBox>
        ///     <ev:Extensions.GroupStyles>
        ///       <ev:GroupStyleCollection>
        ///         <GroupStyle />
        ///       </ev:GroupStyleCollection>
        ///     </ev:Extensions.GroupStyles>
        ///   </ListBox>
        /// ]]>
        /// </example>
        public static readonly DependencyProperty GroupStylesProperty =
            DependencyProperty.RegisterAttached ("GroupStyles", typeof (GroupStyleCollection), typeof (Extensions), new PropertyMetadata (Extensions.OnGroupStylesChanged));
                
        /// <summary>
        /// Identifies the DraveMove attached property.
        /// </summary>
        /// <remarks>
        /// When DragMove is set on a <see cref="Window"/>, then any unhandled left click and drag moves the <see cref="Window"/>.
        /// </remarks>
        public static readonly DependencyProperty DragMoveProperty =
            DependencyProperty.RegisterAttached ("DragMove", typeof (bool), typeof (Extensions), new PropertyMetadata (Extensions.OnDragMoveChanged));

        /// <summary>
        /// Identifies the OpenOnFileDrop property.
        /// </summary>
        /// <remarks>
        /// When OpenOnFileDrop is set to true, then file drag and drops invoke the open command.
        /// </remarks>
        public static readonly DependencyProperty OpenOnFileDropProperty =
            DependencyProperty.RegisterAttached ("OpenOnFileDrop", typeof (bool), typeof (Extensions), new PropertyMetadata (Extensions.OnOpenOnFileDropChanged));
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the value of the <see cref="EventBindingsProperty"/> attached property for a specified <see cref="UIElement"/>.
        /// </summary>
        /// <param name="element">The element from which the property value is read.</param>
        public static EventBindingCollection GetEventBindings (UIElement element)
        {
            return (EventBindingCollection)element.GetValue (Extensions.EventBindingsProperty);
        }

        /// <summary>
        /// Sets the value of the <see cref="EventBindingsProperty"/> attached property for a specified <see cref="UIElement"/>.
        /// </summary>
        /// <param name="element">The element to which the attached property is written.</param>
        /// <param name="eventBindings">The event bindings.</param>
        public static void SetEventBindings (UIElement element, EventBindingCollection eventBindings)
        {
            element.SetValue (Extensions.EventBindingsProperty, eventBindings);
        }
        
        /// <summary>
        /// Gets the value of the <see cref="GroupStylesProperty"/> attached property for a specified <see cref="ItemsControl"/>.
        /// </summary>
        /// <param name="itemsControl">The items control from which the property value is read.</param>
        public static GroupStyleCollection GetGroupStyles (ItemsControl itemsControl)
        {
            return (GroupStyleCollection)itemsControl.GetValue (Extensions.GroupStylesProperty);
        }

        /// <summary>
        /// Sets the value of the <see cref="GroupStylesProperty"/> attached property for a specified <see cref="ItemsControl"/>.
        /// </summary>
        /// <param name="itemsControl">The items control to which the attached property is written.</param>
        /// <param name="groupStyles">The group styles.</param>
        public static void SetGroupStyles (ItemsControl itemsControl, GroupStyleCollection groupStyles)
        {
            itemsControl.SetValue (Extensions.GroupStylesProperty, groupStyles);
        }

        /// <summary>
        /// Gets the value of the <see cref="DragMoveProperty"/> attached property for a specified <see cref="Window"/>.
        /// </summary>
        /// <param name="window">The window from which the property value is read.</param>
        public static bool GetDragMove (Window window)
        {
            return (bool)window.GetValue (Extensions.DragMoveProperty);
        }

        /// <summary>
        /// Sets the value of the <see cref="DragMoveProperty"/> attached property for a specified <see cref="Window"/>.
        /// </summary>
        /// <param name="window">The window to which the attached property is written.</param>
        /// <param name="dragMove">if set to <c>true</c> enables drag move on the window.</param>
        public static void SetDragMove (Window window, bool dragMove)
        {
            window.SetValue (Extensions.DragMoveProperty, dragMove);
        }

        /// <summary>
        /// Gets the value of the <see cref="OpenOnFileDropProperty"/> attached property for a specified <see cref="DependencyObject"/>.
        /// </summary>
        /// <param name="dependencyObject">The dependency object to which the attached property is written.</param>
        public static bool GetOpenOnFileDrop (DependencyObject dependencyObject)
        {
            return (bool)dependencyObject.GetValue (Extensions.OpenOnFileDropProperty);
        }

        /// <summary>
        /// Sets the value of the <see cref="OpenOnFileDropProperty"/> attached property for a specified <see cref="DependencyObject"/>.
        /// </summary>
        /// <param name="dependencyObject">The dependency object to which the attached property is written.</param>
        /// <param name="openOnFileDrop">if set to <c>true</c> invoke the open command on file drop.</param>
        public static void SetOpenOnFileDrop (DependencyObject dependencyObject, bool openOnFileDrop)
        {
            dependencyObject.SetValue (Extensions.OpenOnFileDropProperty, openOnFileDrop);
        }
        
        /// <summary>
        /// Adds the elements of the specified collection to the end of the <see cref="ICollection&lt;T&gt;"/>.
        /// </summary>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="items">The items to add.</param>
        //public static void AddRange<T> (this ICollection<T> collection, IEnumerable<T> items) ** Uncomment when we have C# 3.0 support **
        public static void AddRange<T> (ICollection<T> collection, IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                collection.Add (item);
            }
        }

        /// <summary>
        /// Enumerates all <see cref="ResourceDictionary"/> instances that have been merged with this <see cref="ResourceDictionary"/> instance.
        /// </summary>
        /// <param name="resources">The root resource dictionary.</param>
        /// <returns>All resource dictionaries that have been merged, plus this resource dictionary.</returns>
        //public static IEnumerable<ResourceDictionary> EnumerateResourceDictionaries (this ResourceDictionary resources) ** Uncomment when we have C# 3.0 support **
        public static IEnumerable<ResourceDictionary> EnumerateResourceDictionaries (ResourceDictionary resources)
        {
            yield return resources;

            if (resources.MergedDictionaries != null)
            {
                for (int i = resources.MergedDictionaries.Count - 1; i >= 0; i--)
                {
                    //foreach (ResourceDictionary resourceDictionary in resources.MergedDictionaries[i].EnumerateResourceDictionaries ()) ** Uncomment when we have C# 3.0 support **
                    foreach (ResourceDictionary resourceDictionary in Extensions.EnumerateResourceDictionaries (resources.MergedDictionaries[i]))
                    {
                        yield return resourceDictionary;
                    }
                }
            }
        }

        /// <summary>
        /// Finds a resource by its key. This method searches merged dictionaries as well.
        /// </summary>
        /// <param name="resources">The root resource dictionary.</param>
        /// <param name="resourceKey">The resource key.</param>
        /// <returns>The resource if found, otherwise null.</returns>
        //public static object FindResource (this ResourceDictionary resources, object resourceKey) ** Uncomment when we have C# 3.0 support **
        public static object FindResource (ResourceDictionary resources, object resourceKey)
        {
            //foreach (ResourceDictionary resourceDictionary in resources.EnumerateResourceDictionaries ()) ** Uncomment when we have C# 3.0 support **
            foreach (ResourceDictionary resourceDictionary in Extensions.EnumerateResourceDictionaries (resources))
            {
                object resource = resourceDictionary[resourceKey];
                if (resource != null)
                {
                    return resource;
                }
            }
            return null;
        }

        //public static void ResolveDeferredResources (this ResourceDictionary resources)
        //{
        //    DeferredResource.ResolveDeferredResources (resources);
        //}

        /// <summary>
        /// Resolves all deferred styles in the specified <see cref="ResourceDictionary"/>.
        /// </summary>
        /// <param name="resources">The resource dictionary for which to resolve deferred styles.</param>
        //public static void ResolveDeferredStyles (this ResourceDictionary resources) ** Uncomment when we have C# 3.0 support **
        public static void ResolveDeferredStyles (ResourceDictionary resources)
        {
            DeferredStyle.ResolveDeferredStyles (resources);
        }

        /// <summary>
        /// Evaluates the specified property path against the specified source object.
        /// </summary>
        /// <param name="source">The source object.</param>
        /// <param name="path">The property path.</param>
        /// <param name="formatArgs">Any format args to apply to the specified property path.</param>
        /// <returns>The value of the property path.</returns>
        //public static object EvaluatePropertyPath (this object source, string path, params object[] formatArgs) ** Uncomment when we have C# 3.0 support **
        public static object EvaluatePropertyPath (object source, string path, params object[] formatArgs)
        {
            // Set up a one time binding to help evaluate the property path
            Binding binding = new Binding (string.Format (path, formatArgs));
            binding.Source = source;
            binding.Mode = BindingMode.OneTime;

            // Create a bindable container and bind the property path
            BindableContainer bindableContainer = new BindableContainer ();
            BindingOperations.SetBinding (bindableContainer, BindableContainer.ValueProperty, binding);

            // Get the bindable container value, which will contain the value of the property path
            object value = bindableContainer.Value;

            // Clear the binding from the bindable container
            BindingOperations.ClearAllBindings (bindableContainer);

            // Return the value
            return value;
        }

        /// <summary>
        /// Enumerates the visual descendents.
        /// </summary>
        /// <param name="dependencyObject">The dependency object.</param>
        /// <returns>The visual descendents.</returns>
        //public static IEnumerable<DependencyObject> EnumerateVisualDescendents (this DependencyObject dependencyObject) ** Uncomment when we have C# 3.0 support **
        public static IEnumerable<DependencyObject> EnumerateVisualDescendents (DependencyObject dependencyObject)
        {
            yield return dependencyObject;

            //foreach (DependencyObject child in dependencyObject.EnumerateVisualChildren ()) ** Uncomment when we have C# 3.0 support **
            foreach (DependencyObject child in Extensions.EnumerateVisualChildren (dependencyObject))
            {
                //foreach (DependencyObject descendent in child.EnumerateVisualDescendents ()) ** Uncomment when we have C# 3.0 support **
                foreach (DependencyObject descendent in Extensions.EnumerateVisualDescendents (child))
                {
                    yield return descendent;
                }
            }
        }

        /// <summary>
        /// Enumerates the visual children.
        /// </summary>
        /// <param name="dependencyObject">The dependency object.</param>
        /// <returns>The visual children.</returns>
        //public static IEnumerable<DependencyObject> EnumerateVisualChildren (this DependencyObject dependencyObject) ** Uncomment when we have C# 3.0 support **
        public static IEnumerable<DependencyObject> EnumerateVisualChildren (DependencyObject dependencyObject)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount (dependencyObject); i++)
            {
                yield return VisualTreeHelper.GetChild (dependencyObject, i);
            }
        }

        /// <summary>
        /// Flushes all bindings for the specified <see cref="DependencyObject"/> and all of its descendents.
        /// </summary>
        /// <param name="dependencyObject">The dependency object.</param>
        //public static void FlushBindings (this DependencyObject dependencyObject) ** Uncomment when we have C# 3.0 support **
        public static void FlushBindings (DependencyObject dependencyObject)
        {
            //foreach (DependencyObject element in dependencyObject.EnumerateVisualDescendents ()) ** Uncomment when we have C# 3.0 support **
            foreach (DependencyObject element in Extensions.EnumerateVisualDescendents (dependencyObject))
            {
                LocalValueEnumerator localValueEnumerator = element.GetLocalValueEnumerator ();
                while (localValueEnumerator.MoveNext ())
                {
                    BindingExpressionBase bindingExpression = BindingOperations.GetBindingExpressionBase (element, localValueEnumerator.Current.Property);
                    if (bindingExpression != null)
                    {
                        bindingExpression.UpdateSource ();
                        bindingExpression.UpdateTarget ();
                    }
                }
            }
        }
        #endregion

        #region Private Methods
        private static void OnEventBindingsChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uiElement = d as UIElement;
            if (uiElement != null)
            {
                EventBindingCollection oldEventBindingCollection = e.OldValue as EventBindingCollection;
                EventBindingCollection newEventBindingCollection = e.NewValue as EventBindingCollection;

                // Detach the old instance
                if (oldEventBindingCollection != null)
                {
                    oldEventBindingCollection.SourceUIElement = null;
                }

                // Attach the new instance
                if (newEventBindingCollection != null)
                {
                    newEventBindingCollection.SourceUIElement = uiElement;
                }
            }
        }

        private static void OnGroupStylesChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ItemsControl itemsControl = d as ItemsControl;
            if (itemsControl != null)
            {
                itemsControl.GroupStyle.Clear ();
                GroupStyleCollection groupStyles = e.NewValue as GroupStyleCollection;
                if (groupStyles != null)
                {
                    // itemsControl.GroupStyle.AddRange (groupStyles); ** Uncomment when we have C# 3.0 support **
                    Extensions.AddRange (itemsControl.GroupStyle, groupStyles);
                }
            }
        }

        private static void OnDragMoveChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != e.NewValue)
            {
                bool enabled = (bool)e.NewValue;

                Window window = d as Window;
                if (window != null)
                {
                    if (enabled)
                    {
                        window.MouseLeftButtonDown += Extensions.OnWindowMouseLeftButtonDown;
                    }
                    else
                    {
                        window.MouseLeftButtonDown -= Extensions.OnWindowMouseLeftButtonDown;
                    }
                }
            }
        }

        private static void OnWindowMouseLeftButtonDown (object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Window window = sender as Window;
            if (window != null)
            {
                window.DragMove ();
            }
        }

        private static void OnOpenOnFileDropChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != e.NewValue)
            {
                UIElement uiElement = d as UIElement;
                IInputElement inputElement = d as IInputElement;

                if (uiElement != null && inputElement != null)
                {
                    bool? oldValue = e.OldValue as bool?;
                    bool? newValue = e.NewValue as bool?;

                    if (oldValue != null && oldValue.Value)
                    {
                        uiElement.DragOver -= Extensions.OnUIElementDragOver;
                        uiElement.Drop -= Extensions.OnUIElementDrop;
                    }

                    if (newValue != null && newValue.Value)
                    {
                        uiElement.DragOver += Extensions.OnUIElementDragOver;
                        uiElement.Drop += Extensions.OnUIElementDrop;
                    }
                }
            }
        }

        private static void OnUIElementDragOver (object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;

            IInputElement inputElement = sender as IInputElement;
            if (inputElement != null)
            {
                bool canOpen;
                string filePath = Extensions.GetFilePath (e.Data, inputElement, out canOpen);

                if (canOpen)
                {
                    e.Effects = DragDropEffects.Copy;
                }
            }
        }

        private static void OnUIElementDrop (object sender, DragEventArgs e)
        {
            IInputElement inputElement = sender as IInputElement;
            if (inputElement != null)
            {
                bool canOpen;
                string filePath = Extensions.GetFilePath (e.Data, inputElement, out canOpen);

                if (canOpen)
                {
                    ApplicationCommands.Open.Execute (filePath, inputElement);
                }
            }
        }

        private static string GetFilePath (IDataObject dataObject, IInputElement inputElement, out bool canOpen)
        {
            string filePath = Extensions.GetFilePath (dataObject);
            canOpen = !string.IsNullOrEmpty (filePath) && ApplicationCommands.Open.CanExecute (filePath, inputElement);
            return filePath;
        }

        private static string GetFilePath (IDataObject dataObject)
        {
            if (dataObject != null && dataObject.GetDataPresent (DataFormats.FileDrop, true))
            {
                string[] filePaths = dataObject.GetData (DataFormats.FileDrop, true) as string[];
                if (filePaths != null && filePaths.Length == 1)
                {
                    string filePath = filePaths[0];
                    if (File.Exists (filePath))
                    {
                        return filePath;
                    }
                }
            }

            return null;
        }
        #endregion
    }
}
