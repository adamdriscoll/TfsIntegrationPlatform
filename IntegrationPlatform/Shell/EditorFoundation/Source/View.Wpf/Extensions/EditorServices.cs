// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Windows;
using Microsoft.TeamFoundation.Migration.Shell.Extensibility;
using Microsoft.TeamFoundation.Migration.Shell.Search;
using Microsoft.TeamFoundation.Migration.Shell.Undo;
using Microsoft.TeamFoundation.Migration.Shell.Validation;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Represents a service that provides properties and events to control the Editor Foundation services.
    /// </summary>
    public static class EditorServices
    {
        #region Undo/Redo
        private static readonly DependencyPropertyKey undoManagerPropertyKey = DependencyProperty.RegisterAttachedReadOnly ("UndoManager", typeof (IEditorUndoManager), typeof (EditorServices), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Identifies the UndoManager dependency property.
        /// </summary>
        public static readonly DependencyProperty UndoManagerProperty = EditorServices.undoManagerPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the value of the UndoManager attached property for an object. 
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>The object's UndoManager property value.</returns>
        //public static IEditorUndoManager GetUndoManager (this DependencyObject obj) ** Uncomment when we have C# 3.0 support **
        public static IEditorUndoManager GetUndoManager (DependencyObject obj)
        {
            return (IEditorUndoManager)obj.GetValue (EditorServices.UndoManagerProperty);
        }

        /// <summary>
        /// Creates an undoable region.
        /// </summary>
        /// <param name="obj">The object from which the UndoManager is retrieved.</param>
        /// <returns>An undoable region.</returns>
        //public static UndoableRegion CreateUndoableRegion (this DependencyObject obj) ** Uncomment when we have C# 3.0 support **
        public static UndoableRegion CreateUndoableRegion (DependencyObject obj)
        {
            UndoableRegion undoableRegion = null;
            //UndoableGroup undoManager = obj.GetUndoManager () as UndoableGroup; ** Uncomment when we have C# 3.0 support **
            UndoableGroup undoManager = EditorServices.GetUndoManager (obj) as UndoableGroup;
            if (undoManager != null)
            {
                undoableRegion = new UndoableRegion (undoManager);
            }
            return undoableRegion;
        }

        internal static void SetUndoManager (DependencyObject obj, IEditorUndoManager undoManager)
        {
            obj.SetValue (EditorServices.undoManagerPropertyKey, undoManager);
        }
        #endregion

        #region Search
        private static readonly DependencyPropertyKey searchEnginePropertyKey = DependencyProperty.RegisterAttachedReadOnly ("SearchEngine", typeof (IEditorSearchEngine), typeof (EditorServices), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Identifies the SearchEngine dependency property.
        /// </summary>
        public static readonly DependencyProperty SearchEngineProperty = EditorServices.searchEnginePropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the value of the SearchEngine attached property for an object. 
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>The object's SearchEngine property value.</returns>
        //public static IEditorSearchEngine GetSearchEngine (this DependencyObject obj) ** Uncomment when we have C# 3.0 support **
        public static IEditorSearchEngine GetSearchEngine (DependencyObject obj)
        {
            return (IEditorSearchEngine)obj.GetValue (EditorServices.SearchEngineProperty);
        }

        internal static void SetSearchEngine (DependencyObject obj, IEditorSearchEngine searchEngine)
        {
            obj.SetValue (EditorServices.searchEnginePropertyKey, searchEngine);
        }
        #endregion

        #region Validation
        private static readonly DependencyPropertyKey validationManagerPropertyKey = DependencyProperty.RegisterAttachedReadOnly ("ValidationManager", typeof (IEditorValidationManager), typeof (EditorServices), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Identifies the ValidationManager dependency property.
        /// </summary>
        public static readonly DependencyProperty ValidationManagerProperty = EditorServices.validationManagerPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the value of the ValidationManager attached property for an object. 
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>The object's ValidationManager property value.</returns>
        //public static IEditorValidationManager GetValidationManager (this DependencyObject obj) ** Uncomment when we have C# 3.0 support **
        public static IEditorValidationManager GetValidationManager (DependencyObject obj)
        {
            return (IEditorValidationManager)obj.GetValue (EditorServices.ValidationManagerProperty);
        }

        internal static void SetValidationManager (DependencyObject obj, IEditorValidationManager validationManager)
        {
            obj.SetValue (EditorServices.validationManagerPropertyKey, validationManager);
        }
        #endregion

        #region Plugins
        private static readonly DependencyPropertyKey pluginManagerPropertyKey = DependencyProperty.RegisterAttachedReadOnly ("PluginManager", typeof (IPluginManager), typeof (EditorServices), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Identifies the PluginManager dependency property.
        /// </summary>
        public static readonly DependencyProperty PluginManagerProperty = EditorServices.pluginManagerPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the value of the PluginManager attached property for an object. 
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>The object's PluginManager property value.</returns>
        //public static IPluginManager GetPluginManager (this DependencyObject obj) ** Uncomment when we have C# 3.0 support **
        public static IPluginManager GetPluginManager (DependencyObject obj)
        {
            return (IPluginManager)obj.GetValue (EditorServices.PluginManagerProperty);
        }

        internal static void SetPluginManager (DependencyObject obj, IPluginManager pluginManager)
        {
            obj.SetValue (EditorServices.pluginManagerPropertyKey, pluginManager);
        }
        #endregion
    }
}
