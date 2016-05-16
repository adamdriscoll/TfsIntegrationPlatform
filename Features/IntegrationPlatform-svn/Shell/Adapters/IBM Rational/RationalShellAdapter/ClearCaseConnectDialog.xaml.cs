// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ClearCase;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;

namespace Microsoft.TeamFoundation.Migration.Shell.RationalShellAdapter
{
    /// <summary>
    /// Interaction logic for ClearCaseConnectDialog.xaml
    /// </summary>
    public partial class ClearCaseConnectDialog : Window
    {
        private ClearCaseMigrationSourceViewModel m_source;

        public ClearCaseConnectDialog(MigrationSource source)
        {
            InitializeComponent();
            m_source = new ClearCaseMigrationSourceViewModel(source);
            DataContext = m_source;
            Closed += new EventHandler(ClearCaseConnectDialog_Closed);
        }

        void ClearCaseConnectDialog_Closed(object sender, EventArgs e)
        {
            m_source.Dispose();
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UIHelper.AdjustLastColumnWidth(sender as ListView);
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            m_source.Save();
            DialogResult = true;
            Close();
        }

        private void browseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Choose a directory for the Download Path:";
            dialog.ShowNewFolderButton = true;
            dialog.SelectedPath = m_source.DownloadFolder;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                m_source.DownloadFolder = dialog.SelectedPath;
            }
        }

        private void downloadFolderTextBox_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            downloadFolderTextBox.SelectAll();
        }
    }

    public class ClearCaseVOBViewModel
    {
        private CCVOB m_vob;

        public ClearCaseVOBViewModel(CCVOB vob)
        {
            m_vob = vob;
            Name = m_vob.TagName;
        }

        public string Name { get; private set; }
    }

    public class ClearCaseViewViewModel : INotifyPropertyChanged
    {
        private ClearCaseMigrationSourceViewModel m_source;

        public ClearCaseViewViewModel(CCView view, ClearCaseMigrationSourceViewModel source)
        {
            CCView = view;
            TagName = CCView.TagName;
            m_source = source;
        }

        public CCView CCView { get; private set; }

        private bool m_isLoaded = false;
        public bool IsLoaded
        {
            get
            {
                return m_isLoaded;
            }
            set
            {
                if (m_isLoaded != value)
                {
                    m_isLoaded = value;
                    OnPropertyChanged("IsLoaded");
                }
            }
        }

        private bool m_isLoading = false;
        public bool IsLoading
        {
            get
            {
                return m_isLoading;
            }
            set
            {
                if (m_isLoading != value)
                {
                    m_isLoading = value;
                    OnPropertyChanged("IsLoading");
                }
            }
        }

        public void Load()
        {
            if (!IsLoaded)
            {
                m_source.QueueLoad(this);
            }
        }

        private bool m_canEditVob = false;
        public bool CanEditVob
        {
            get
            {
                return m_canEditVob;
            }
            set
            {
                m_canEditVob = value;
                OnPropertyChanged("CanEditVob");
            }
        }

        private string m_primaryVob;
        public string PrimaryVob
        {
            get
            {
                return m_primaryVob;
            }
            set
            {
                m_primaryVob = value;
                OnPropertyChanged("PrimaryVob");
            }
        }

        private string m_storageLocation;
        public string StorageLocation
        {
            get
            {
                return m_storageLocation;
            }
            set
            {
                m_storageLocation = value;
                OnPropertyChanged("StorageLocation");
            }
        }

        private string m_storageLocationLocalPath;
        public string StorageLocationLocalPath
        {
            get
            {
                return m_storageLocationLocalPath;
            }
            set
            {
                m_storageLocationLocalPath = value;
                OnPropertyChanged("StorageLocationLocalPath");
            }
        }

        public string TagName { get; private set; }

        private ViewTypeEnum? m_viewType;
        public ViewTypeEnum? ViewType
        {
            get
            {
                return m_viewType;
            }
            set
            {
                if (m_viewType != value)
                {
                    m_viewType = value;
                    OnPropertyChanged("ViewType");
                }
            }
        }

        public enum ViewTypeEnum
        {
            Dynamic,
            Snapshot
        }

        public void ParseStoragePaths(string str)
        {
            Match globalPath = Regex.Match(str, "(Global path: )(.*)(" + Environment.NewLine + ")");
            StorageLocation = globalPath.Groups[2].Value;

            Match localPath = Regex.Match(str, "(View server access path: )(.*)(" + Environment.NewLine + ")");
            StorageLocationLocalPath = localPath.Groups[2].Value;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion
    }

    public class ClearCaseMigrationSourceViewModel : INotifyPropertyChanged, IDisposable
    {
        private static ClearToolClass s_clearCaseTool = new ClearToolClass();
        
        private MigrationSource m_migrationSource;
        private BackgroundWorker m_worker;

        public bool DetectChangesInCC { get; set; }

        public bool LabelAllVersions { get; set; }

        private string m_downloadFolder;
        public string DownloadFolder
        {
            get
            {
                return m_downloadFolder;
            }
            set
            {
                m_downloadFolder = value;
                OnPropertyChanged("DownloadFolder");
                OnPropertyChanged("IsDownloadFolderValid");
            }
        }

        public bool IsDownloadFolderValid
        {
            get
            {
                try
                {
                    string fullPath = Path.GetFullPath(DownloadFolder);
                    return Path.Equals(fullPath, DownloadFolder);
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }
        }

        public ClearfsimportConfiguration ClearfsimportConfiguration { get; private set; }

        public List<DriveInfo> LocalDrives { get; private set; }

        private DriveInfo m_selectedLocalDrive;
        public DriveInfo SelectedLocalDrive
        {
            get
            {
                if (m_selectedLocalDrive == null)
                {
                    m_selectedLocalDrive = LocalDrives.FirstOrDefault();
                }
                return m_selectedLocalDrive;
            }
            set
            {
                m_selectedLocalDrive = value;
            }
        }

        private string GetCustomSetting(MigrationSource migrationSource, string key)
        {
            return migrationSource.CustomSettings.CustomSetting.Where(x => x.SettingKey.Equals(key)).Select(x => x.SettingValue).FirstOrDefault();
        }

        private DispatcherTimer m_timer = new DispatcherTimer();
        public ClearCaseMigrationSourceViewModel(MigrationSource migrationSource)
        {
            m_timer.Interval = TimeSpan.FromSeconds(1);
            m_timer.Tick += new EventHandler(m_timer_Tick);

            m_migrationSource = migrationSource;

            // get initial conditions
            DetectChangesInCC = string.Equals(GetCustomSetting(migrationSource, CCResources.DetectChangesInCC), "True", StringComparison.OrdinalIgnoreCase);
            LabelAllVersions = string.Equals(GetCustomSetting(migrationSource, CCResources.LabelAllVersions), "True", StringComparison.OrdinalIgnoreCase);
            DownloadFolder = GetCustomSetting(migrationSource, CCResources.DownloadFolderSettingName);
            ClearfsimportConfiguration = new ClearfsimportConfiguration();
            ClearfsimportConfiguration.Unco = string.Equals(GetCustomSetting(migrationSource, CCResources.DetectChangesInCC), "True", StringComparison.OrdinalIgnoreCase);
            ClearfsimportConfiguration.Master = string.Equals(GetCustomSetting(migrationSource, CCResources.DetectChangesInCC), "True", StringComparison.OrdinalIgnoreCase);
            ClearfsimportConfiguration.ParseOutput = string.Equals(GetCustomSetting(migrationSource, CCResources.DetectChangesInCC), "True", StringComparison.OrdinalIgnoreCase);
            string batchSizeString = GetCustomSetting(migrationSource, CCResources.DetectChangesInCC);
            int batchSize;
            if (int.TryParse(batchSizeString, out batchSize))
            {
                ClearfsimportConfiguration.BatchSize = batchSize;
            }

            LocalDrives = DriveInfo.GetDrives().Where(x => x.DriveType == DriveType.Network).ToList();
            
            m_worker = new BackgroundWorker();
            m_worker.WorkerReportsProgress = true;
            m_worker.DoWork += new DoWorkEventHandler(m_worker_DoWork);
            m_worker.ProgressChanged += new ProgressChangedEventHandler(m_worker_ProgressChanged);
            m_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_worker_RunWorkerCompleted);
        }

        void m_timer_Tick(object sender, EventArgs e)
        {
            Debug.WriteLine("Timer ticked");
            m_timer.Stop();
            LoadQueued();
        }

        private bool m_isLoading = false;
        public bool IsLoading
        {
            get
            {
                return m_isLoading;
            }
            private set
            {
                if (m_isLoading != value)
                {
                    m_isLoading = value;
                    OnPropertyChanged("IsLoading");
                }
            }
        }

        private ObservableCollection<ClearCaseViewViewModel> m_views;
        public ObservableCollection<ClearCaseViewViewModel> Views
        {
            get
            {
                if (m_views == null)
                {
                    m_views = new ObservableCollection<ClearCaseViewViewModel>();
                    if (!m_worker.IsBusy)
                    {
                        IsLoading = true;
                        m_worker.RunWorkerAsync();
                    }
                }
                return m_views;
            }
        }

        private ObservableCollection<string> m_vobs;
        public ObservableCollection<string> Vobs
        {
            get
            {
                if (m_vobs == null)
                {
                    m_vobs = new ObservableCollection<string>();
                }
                return m_vobs;
            }
        }

        private ClearCaseViewViewModel m_selectedView;
        public ClearCaseViewViewModel SelectedView
        {
            get
            {
                return m_selectedView;
            }
            set
            {
                if (m_selectedView != value)
                {
                    m_selectedView = value;
                    if (m_selectedView != null)
                    {
                        m_selectedView.Load();
                    }
                    OnPropertyChanged("SelectedView");
                }
            }
        }

        private int m_loadProgress;
        public int LoadProgress
        {
            get
            {
                return m_loadProgress;
            }
            private set
            {
                m_loadProgress = value;
                OnPropertyChanged("LoadProgress");
            }
        }

        void m_worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            LoadProgress = e.ProgressPercentage;
        }

        void m_worker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (e.Argument is ClearCaseViewViewModel)
            {
                ClearCaseViewViewModel view = e.Argument as ClearCaseViewViewModel;
                if (!view.IsLoaded)
                {
                    view.IsLoading = true;
                    view.ViewType = view.CCView.IsSnapShot ? ClearCaseViewViewModel.ViewTypeEnum.Snapshot : ClearCaseViewViewModel.ViewTypeEnum.Dynamic;

                    if (view.ViewType == ClearCaseViewViewModel.ViewTypeEnum.Snapshot)
                    {
                        view.ParseStoragePaths(s_clearCaseTool.CmdExec("lsview -long " + view.TagName));
                    }

                    try
                    {
                        view.PrimaryVob = view.CCView.Stream.ProjectVOB.TagName;
                    }
                    catch
                    {
                        view.CanEditVob = true;
                    }

                    view.IsLoaded = true;
                    view.IsLoading = false;
                }
            }
            else
            {
                m_worker.ReportProgress(0);

                ApplicationClass applicationClass = new ApplicationClass();

                m_worker.ReportProgress(33);

                CCViews views = applicationClass.get_Views(true, string.Empty);
                List<ClearCaseViewViewModel> viewsList = new List<ClearCaseViewViewModel>();
                foreach (CCView view in views)
                {
                    viewsList.Add(new ClearCaseViewViewModel(view, this));
                }

                m_worker.ReportProgress(67);

                CCVOBs vobs = applicationClass.get_VOBs(true, string.Empty);
                List<ClearCaseVOBViewModel> vobsList = new List<ClearCaseVOBViewModel>();
                foreach (CCVOB vob in vobs)
                {
                    vobsList.Add(new ClearCaseVOBViewModel(vob));
                }

                m_worker.ReportProgress(100);

                object[] objs = new object[] { viewsList.OrderBy(x => x.TagName), vobsList.OrderBy(x => x.Name) };
                e.Result = objs;
            }
        }

        void m_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (m_disposeRequested)
            {
                Dispose();
                return;
            }
            if (e.Error == null)
            {
                if (e.Result is object[])
                {
                    object[] objs = e.Result as object[];
                    IEnumerable<ClearCaseViewViewModel> viewsList = objs[0] as IEnumerable<ClearCaseViewViewModel>;
                    if (viewsList != null)
                    {
                        foreach (ClearCaseViewViewModel view in viewsList)
                        {
                            Views.Add(view);
                        }
                    }
                    IEnumerable<ClearCaseVOBViewModel> vobsList = objs[1] as IEnumerable<ClearCaseVOBViewModel>;
                    if (vobsList != null)
                    {
                        foreach (ClearCaseVOBViewModel vob in vobsList)
                        {
                            Vobs.Add(vob.Name);
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("Finished loading");
                }
            }
            else
            {
                Utilities.HandleException(e.Error);
            }
            LoadQueued();
            IsLoading = false;
        }

        private object m_lockObject = new object();
        private ClearCaseViewViewModel m_queuedView;
        public void QueueLoad(ClearCaseViewViewModel view)
        {
            lock (m_lockObject)
            {
                m_queuedView = view;
                Debug.WriteLine("Queueing view: " + m_queuedView.TagName);
                if (!m_worker.IsBusy)
                {
                    Debug.WriteLine("Timer started: " + m_queuedView.TagName);
                    m_timer.Stop();
                    m_timer.Start();
                }
            }
        }

        private void LoadQueued()
        {
            lock (m_lockObject)
            {
                if (m_queuedView != null && !m_worker.IsBusy)
                {
                    ClearCaseViewViewModel nextView = m_queuedView;
                    m_queuedView = null;
                    Debug.WriteLine("Loading view: " + nextView.TagName);
                    m_worker.RunWorkerAsync(nextView);
                }
            }
        }

        public void Save()
        {
            m_migrationSource.ServerUrl = SelectedView.PrimaryVob;
            m_migrationSource.SourceIdentifier = SelectedView.PrimaryVob;
            m_migrationSource.FriendlyName = SelectedView.PrimaryVob;
            m_migrationSource.ServerIdentifier = SelectedView.PrimaryVob;

            m_migrationSource.CustomSettings.CustomSetting.Clear();

            SaveCustomSetting(CCResources.PrecreatedViewSettingName, SelectedView.TagName);

            if (SelectedView.ViewType == ClearCaseViewViewModel.ViewTypeEnum.Dynamic)
            {
                SaveCustomSetting(CCResources.DynamicViewRootSettingName, SelectedLocalDrive.Name);
            }
            else
            {
                SaveCustomSetting(CCResources.StorageLocationSettingName, SelectedView.StorageLocation);
                SaveCustomSetting(CCResources.StorageLocationLocalPathSettingName, SelectedView.StorageLocationLocalPath);
            }

            SaveCustomSetting(CCResources.DownloadFolderSettingName, DownloadFolder);
            SaveCustomSetting(CCResources.DetectChangesInCC, DetectChangesInCC.ToString());
            SaveCustomSetting(CCResources.LabelAllVersions, LabelAllVersions.ToString());
            SaveCustomSetting(CCResources.ClearfsimportConfigurationUnco, ClearfsimportConfiguration.Unco.ToString());
            SaveCustomSetting(CCResources.ClearfsimportConfigurationMaster, ClearfsimportConfiguration.Master.ToString());
            SaveCustomSetting(CCResources.ClearfsimportConfigurationParseOutput, ClearfsimportConfiguration.ParseOutput.ToString());
            SaveCustomSetting(CCResources.ClearfsimportConfigurationBatchSize, ClearfsimportConfiguration.BatchSize.ToString());
        }

        /// <summary>
        /// Save key-value pair to CustomSettings.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>Item already exists.</returns>
        private bool SaveCustomSetting(string key, string value)
        {
            CustomSetting userNameSetting = m_migrationSource.CustomSettings.CustomSetting.FirstOrDefault(x => string.Equals(x.SettingKey, key));
            if (userNameSetting != null)
            {
                userNameSetting.SettingValue = value;
                return true;
            }
            else
            {
                CustomSetting newUserNameSetting = new CustomSetting();
                newUserNameSetting.SettingKey = key;
                newUserNameSetting.SettingValue = value;
                m_migrationSource.CustomSettings.CustomSetting.Add(newUserNameSetting);
                return false;
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion

        #region IDisposable Members

        private bool m_disposeRequested = false;
        public void Dispose()
        {
            if (m_worker != null)
            {
                if (m_worker.IsBusy)
                {
                    m_disposeRequested = true;
                    return;
                }

                m_worker.DoWork -= m_worker_DoWork;
                m_worker.RunWorkerCompleted -= m_worker_RunWorkerCompleted;
                m_worker.Dispose();
                m_worker = null;
            }
        }

        #endregion
    }
}
