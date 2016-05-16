// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    /// <summary>
    /// Interaction logic for ServersView.xaml
    /// </summary>
    public partial class ProvidersView : UserControl
    {
        public ProvidersView()
        {
            InitializeComponent();
        }

        private void OnAddProviderCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.CanAddProvider();
        }

        private void OnAddProviderExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.AddProvider(e.Parameter as ProviderElement);
        }

        private void OnDeleteCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.CanDelete();
        }

        private void OnDeleteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.Delete();
        }

        private void OnCutCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.CanCut();
        }

        private void OnCutExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.Cut();
        }

        private void OnCopyCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.CanCopy();
        }

        private void OnCopyExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.Copy();
        }

        private void OnPasteCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.CanPaste();
        }

        private void OnPasteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.Paste();
        }

        private bool CanAddProvider()
        {
            return this.DataContext is Providers;
        }

        // TODO: This whole thing becomes add a session, not add a provider
        private void AddProvider(ProviderElement provider)
        {
            Providers myData = this.DataContext as Providers;
            if (myData != null)
            {
                if (provider == null)
                {
                    provider = new ProviderElement();
                    provider.FriendlyName = "TFS 2005 Migration Provider";
                    provider.ReferenceName = "GUID";
                }

                myData.Provider.Add(provider);
            }
        }

        private bool CanDelete()
        {
            return this.providersListBox.SelectedItem != null;
        }

        private void Delete()
        {
            Providers myData = this.DataContext as Providers;
            if (myData != null)
            {
                myData.Provider.Remove((ProviderElement)this.providersListBox.SelectedItem);
            }
        }

        private bool CanCut()
        {
            return this.CanCopy() && this.CanDelete();
        }

        private void Cut()
        {
            this.Copy();
            this.Delete();
        }

        private bool CanCopy()
        {
            return this.providersListBox.SelectedItem != null;
        }

        private void Copy()
        {
            DataObject dataObject = new DataObject();
            dataObject.SetData(this.providersListBox.SelectedItem);

            DataObjectSettingDataEventArgs settingDataEventArgs = new DataObjectSettingDataEventArgs(dataObject, this.providersListBox.SelectedItem.GetType().FullName);
            this.RaiseEvent(settingDataEventArgs);

            if (settingDataEventArgs.CommandCancelled)
            {
                return;
            }

            DataObjectCopyingEventArgs copyingEventArgs = new DataObjectCopyingEventArgs(dataObject, false);
            this.RaiseEvent(copyingEventArgs);

            if (copyingEventArgs.CommandCancelled)
            {
                return;
            }

            Clipboard.SetDataObject(dataObject, true);
        }

        private bool CanPaste()
        {
            IDataObject dataObject = Clipboard.GetDataObject();
            if (dataObject != null)
            {
                return dataObject.GetDataPresent(typeof(ProviderElement));
            }

            return false;
        }

        private void Paste()
        {
            IDataObject dataObject = Clipboard.GetDataObject();
            if (dataObject != null)
            {
                DataObjectPastingEventArgs pastingEventArgs = new DataObjectPastingEventArgs(dataObject, false, typeof(ProviderElement).FullName);
                this.RaiseEvent(pastingEventArgs);

                if (pastingEventArgs.CommandCancelled)
                {
                    return;
                }

                ProviderElement provider = dataObject.GetData(typeof(ProviderElement)) as ProviderElement;
                if (provider != null)
                {
                    Providers myData = this.DataContext as Providers;
                    if (myData != null)
                    {
                        myData.Provider.Add(provider);
                    }
                }
            }
        }
    }
}
