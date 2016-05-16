// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.Shell.View;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.Extensibility
{
    /// <summary>
    /// This class manages loading, unloading, and interfacing with a single Plugin
    /// </summary>
    internal class PluginHandler
    {
        #region Fields
        private readonly Type type;
        private readonly PluginDescriptor descriptor;
        private readonly PluginContextAttribute[] pluginContextAttributes;
        private readonly List<object> availableContexts;
        private IPlugin instance;
        #endregion

        #region Constructors
        private PluginHandler (Type type, PluginDescriptionAttribute pluginDescriptionAttribute)
        {
            this.type = type;

            object[] attributes = type.GetCustomAttributes (typeof (PluginContextAttribute), true);
            this.pluginContextAttributes = new PluginContextAttribute[attributes.Length];
            for (int i = 0; i < attributes.Length; i++)
            {
                this.pluginContextAttributes[i] = (PluginContextAttribute)attributes[i];
            }

            this.descriptor = pluginDescriptionAttribute.PluginDescriptor;
            this.availableContexts = new List<object> ();

            if (this.pluginContextAttributes.Length == 0)
            {
                this.Load ();
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the type of the Plugin.
        /// </summary>
        public Type Type
        {
            get
            {
                return this.type;
            }
        }

        /// <summary>
        /// Gets the Plugin descriptor.
        /// </summary>
        public PluginDescriptor Descriptor
        {
            get
            {
                return this.descriptor;
            }
        }

        /// <summary>
        /// Specifies whether the Plugin is loaded.
        /// </summary>
        public bool IsLoaded
        {
            get
            {
                return this.instance != null;
            }
        }

        private IEnumerable<Type> OptionalContexts
        {
            get
            {
                return this.EnumerateContextTypes (ContextSupport.Optional);
            }
        }

        private IEnumerable<Type> RequiredContexts
        {
            get
            {
                return this.EnumerateContextTypes (ContextSupport.Required);
            }
        }

        private IEnumerable<Type> SupportedContexts
        {
            get
            {
                return this.EnumerateContextTypes (ContextSupport.Optional | ContextSupport.Required);
            }
        }

        private bool AllRequiredContextsAvailable
        {
            get
            {
                foreach (Type requiredContextType in this.RequiredContexts)
                {
                    bool foundMatch = false;
                    foreach (object context in this.availableContexts)
                    {
                        if (requiredContextType.IsAssignableFrom (context.GetType ()))
                        {
                            foundMatch = true;
                            break;
                        }
                    }

                    if (!foundMatch)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when the Plugin is loaded.
        /// </summary>
        public event EventHandler Loaded;

        /// <summary>
        /// Occurs when the Plugin is unloaded.
        /// </summary>
        public event EventHandler Unloaded;
        #endregion

        #region Public Methods
        public void OnContextEnter (object context)
        {
            // Add the context to the list of available contexts
            this.availableContexts.Add (context);

            // If the Plugin is already loaded, send a notification
            if (this.IsLoaded)
            {
                this.instance.OnContextEnter (context);
            }
            // Otherwise load the Plugin and send a notification for each available context
            else
            {
                if (this.AllRequiredContextsAvailable)
                {
                    this.Load ();
                    foreach (object availableContext in this.availableContexts)
                    {
                        this.instance.OnContextEnter (availableContext);
                    }
                }
            }
        }

        public void OnContextLeave (object context)
        {
            // Remove the context from the list of available contexts
            this.availableContexts.Remove (context);

            // If the Plugin is loaded, send a notification
            if (this.IsLoaded)
            {
                this.instance.OnContextLeave (context);

                // If all the required contexts are no longer available, dispose the Plugin
                if (!this.AllRequiredContextsAvailable)
                {
                    if (this.instance is IDisposable)
                    {
                        ((IDisposable)this.instance).Dispose ();
                    }
                    this.instance = null;
                }
            }
        }

        public bool SupportsContext (Type contextType)
        {
            return this.IsTypeCompatible (contextType, this.SupportedContexts);
        }

        public IEnumerable<IConflictTypeView> GetConflictTypeViews()
        {
            return instance.GetConflictTypeViews();
        }

        public IMigrationSourceView GetMigrationSourceView()
        {
            return instance.GetMigrationSourceView();
        }

        public ExecuteFilterStringExtension FilterStringExtension
        {
            get
            {
                return instance.FilterStringExtension;
            }
        }

        public static PluginHandler FromType (Type type)
        {
            object[] attributes = type.GetCustomAttributes (typeof (PluginDescriptionAttribute), false);
            if (attributes.Length > 0)
            {
                return new PluginHandler (type, (PluginDescriptionAttribute)attributes[0]);
            }
            return null;
        }
        #endregion

        #region Private Methods
        private void Load ()
        {
            this.Unload ();
            
            this.instance = (IPlugin)Activator.CreateInstance (this.Type);
            this.RaiseLoadedEvent ();
        }

        private void Unload ()
        {
            if (this.instance != null)
           {                
                if (this.instance is IDisposable)
                {
                    ((IDisposable)this.instance).Dispose ();
                }
                this.RaiseUnloadedEvent ();
            }
        }

        private IEnumerable<Type> EnumerateContextTypes (ContextSupport contextSupport)
        {
            foreach (PluginContextAttribute pluginContextAttribute in this.pluginContextAttributes)
            {
                if ((pluginContextAttribute.ContextSupport & contextSupport) != 0)
                {
                    yield return pluginContextAttribute.ContextType;
                }
            }
        }

        private bool IsTypeCompatible (Type type, IEnumerable<Type> types)
        {
            foreach (Type otherType in types)
            {
                if (otherType.IsAssignableFrom (type))
                {
                    return true;
                }
            }
            return false;
        }

        private void RaiseLoadedEvent ()
        {
            if (this.Loaded != null)
            {
                this.Loaded (this, EventArgs.Empty);
            }
        }

        private void RaiseUnloadedEvent ()
        {
            if (this.Unloaded != null)
            {
                this.Unloaded (this, EventArgs.Empty);
            }
        }
        #endregion
    }
}
