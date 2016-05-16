// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Shell.Extensibility;
using Microsoft.TeamFoundation.Migration.Shell.Model;

namespace Microsoft.TeamFoundation.Migration.Shell.Controller
{
    public class ShellController : ControllerBase<ConfigurationModel>
    {
        #region Public Methods
        /// <summary>
        /// Open an existing Configuration from the underlying migration tools
        /// DB synchronously.
        /// </summary>
        /// <param name="sessionGroupUniqueId">The unique id of the configuration</param>
        /// <returns><c>true</c> if the open completed, <c>false</c> otherwise.</returns>
        public bool Open(Guid sessionGroupUniqueId)
        {
            if (this.Close())
            {
                string guid = sessionGroupUniqueId.ToString();

                OpeningEventArgs openingEventArgs = new OpeningEventArgs(guid);
                this.OnOpeningInternal(openingEventArgs);
                this.RaiseOpeningEvent(openingEventArgs);

                OpenedEventArgs openedEventArgs = null;

                try
                {
                    base.Model = ConfigurationModel.Load(sessionGroupUniqueId);
                    openedEventArgs = new OpenedEventArgs(guid, null);
                }
                catch (Exception exception)
                {
                    openedEventArgs = new OpenedEventArgs(guid, exception);
                }

                try
                {
                    this.OnOpenedInternal(openedEventArgs);
                }
                catch (Exception exception)
                {
                    openedEventArgs = new OpenedEventArgs(guid, exception);
                }

                this.RaiseOpenedEvent(openedEventArgs);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Save the model. 
        /// </summary>
        /// <returns></returns>
        public bool Save(Guid sessionGroupUniqueId, bool saveAsNew)
        {
            if (this.Model == null)
            {
                return false;
            }

            string guid = sessionGroupUniqueId.ToString();

            SavingEventArgs savingEventArgs = new SavingEventArgs(guid);
            this.OnSavingInternal(savingEventArgs);
            this.RaiseSavingEvent(savingEventArgs);

            SavedEventArgs savedEventArgs = null;

            try
            {
                this.Model.Save(saveAsNew);
                savedEventArgs = new SavedEventArgs(guid, null);
            }
            catch (Exception exception)
            {
                savedEventArgs = new SavedEventArgs(guid, exception);
            }

            try
            {
                this.OnSavedInternal(savedEventArgs);
            }
            catch (Exception exception)
            {
                savedEventArgs = new SavedEventArgs(guid, exception);
            }

            this.RaiseSavedEvent(savedEventArgs);

            return true;
        }

        #endregion

        #region Protected Methods
        protected override PluginManager InitializePluginManager()
        {
            PluginManager pluginManager = new PluginManager();
            // Add the controller itself as a plugin context.
            // This will give all plugins full access to the controller.
            pluginManager.PluginContexts.Add(this);

            return pluginManager;
        }
        #endregion
    }
}
