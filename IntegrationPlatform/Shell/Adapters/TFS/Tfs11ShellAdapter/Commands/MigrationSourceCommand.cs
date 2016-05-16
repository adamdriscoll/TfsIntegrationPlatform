// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Shell.View;
using Microsoft.TeamFoundation.Server;

using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Shell.Tfs11ShellAdapter.Properties;

namespace Microsoft.TeamFoundation.Migration.Shell.Tfs11ShellAdapter
{
    public class MigrationSourceCommand : Microsoft.TeamFoundation.Migration.Shell.View.Command
    {
        #region Fields
        private const string m_commandName = "Connect";

        private TfsTeamProjectCollection m_tfs;
        private ProjectInfo m_teamProject;
        #endregion
        
        #region Properties
        public string CommandName 
        {
            get
            {
                return m_commandName;
            }
        }

        public BitmapImage ButtonImage
        {
            get
            {
                return new BitmapImage(new Uri(@"graphics\server.png", UriKind.Relative));
            }
        }
        #endregion

        #region Public Methods
        public override bool CanExecute(object parameter)
        {
            throw new NotImplementedException();
        }

        public override void Execute(object parameter)
        {
            MigrationSource migrationSource = parameter as MigrationSource;

            if (migrationSource == null)
            {
                throw new ArgumentException("Invalid argument: parameter: " + parameter.ToString());
            }

            if (ChooseProject())
            {
                migrationSource.ServerIdentifier = m_tfs.InstanceId.ToString();
                migrationSource.ServerUrl = m_tfs.Uri.ToString();
                migrationSource.FriendlyName = m_tfs.Uri.Host;
                migrationSource.SourceIdentifier = m_teamProject.Name;
            }
            else
            {
                migrationSource.ProviderReferenceName = null;
            }
        }
        #endregion

        #region Private Methods
        private bool ChooseProject()
        {
            // The domain picker is a user control in the Team Foundation OM 
            // for selecting a Team Foundation Server and Team Project.

            TeamProjectPicker domainPicker = new TeamProjectPicker();
            if (m_tfs != null)
            {
                domainPicker.SelectedTeamProjectCollection = m_tfs;
            }

            if (domainPicker.ShowDialog() == DialogResult.OK)
            {
                m_teamProject = null;
                m_tfs = domainPicker.SelectedTeamProjectCollection;

                ProjectInfo[] projects = domainPicker.SelectedProjects;
                if (projects.Length > 0)
                {
                    m_teamProject = projects[0];
                }

                if (IsAzure(m_tfs))
                {
                    ServiceIdentity retrievedIdentity = null;
                    try
                    {
                        IAccessControlService accessControlService = m_tfs.GetService(typeof(IAccessControlService)) as IAccessControlService;
                        retrievedIdentity = accessControlService.ProvisionServiceIdentity();
                    }
                    catch (Exception ex)
                    {
                        string msg = String.Format(CultureInfo.InvariantCulture, Resources.UnableToGetAzureServiceIdentity, ex.Message);
                        TraceManager.TraceError(msg);
                        MessageBox.Show(msg);
                    }

                    if (retrievedIdentity != null)
                    {
                        try
                        {
                            CredentialsCacheManager.StoreCredentials(m_tfs.Uri,
                                                                     retrievedIdentity.IdentityInfo.Name,
                                                                     retrievedIdentity.IdentityInfo.Password,
                                                                     CachedCredentialsType.ServiceIdentity,
                                                                     true);
                        }
                        catch (Exception ex)
                        {
                            string msg = String.Format(CultureInfo.InvariantCulture, Resources.UnableToSaveAzureServiceIdentityLocally, ex.Message);
                            TraceManager.TraceError(msg);
                            MessageBox.Show(msg);
                        }
                    }
                }

                return true;
            }
            else
            {
                m_teamProject = null;
                return false;
            }
        }

        private static bool IsAzure(TfsConnection connection)
        {
            return (connection.ServerCapabilities & Microsoft.TeamFoundation.Client.ServerCapabilities.Hosted) == ServerCapabilities.Hosted;
        }

        #endregion       
    }
}
