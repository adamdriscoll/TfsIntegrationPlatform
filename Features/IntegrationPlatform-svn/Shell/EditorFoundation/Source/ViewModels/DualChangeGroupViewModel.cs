// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Drawing;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Shell.Model;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.Migration.Shell.ViewModel
{
    public class DualChangeGroupViewModel : ModelObject
    {
        private bool m_isSelected = false;
        public bool IsSelected
        {
            get
            {
                return m_isSelected;
            }
            set
            {
                if (m_isSelected != value)
                {
                    m_isSelected = value;
                    RaisePropertyChangedEvent("DisplayColor", null, null);
                }
            }
        }
        public bool IsMilestone { get; set; }
        private RuntimeEntityModel m_context;
        public DualChangeGroupViewModel(RTChangeGroup deltaTables, RTChangeGroup migrationInstructions, int count)
        {
            Name = deltaTables.Name;
            DeltaTables = deltaTables;
            MigrationInstructions = migrationInstructions;
            ChangeActionCount = count;
            IsMilestone = false;

            m_context = RuntimeEntityModel.CreateInstance();

            RuntimeManager host = RuntimeManager.GetInstance();
            IRefreshService refresh = (IRefreshService)host.GetService(typeof(IRefreshService));
            refresh.AutoRefresh += this.AutoRefresh;

            Refresh();

            m_deltaTablesStatus = (ChangeStatus)DeltaTables.Status;
            m_migrationInstructionsStatus = (ChangeStatus)MigrationInstructions.Status;
        }

        public static readonly int s_bucketSize = 1000;
        public int Bucket
        {
            get
            {
                int bucket;
                int.TryParse(Name, out bucket);
                return bucket / s_bucketSize;
            }
        }

        public void AutoRefresh(object sender, EventArgs e)
        {
            Refresh();
        }

        private BackgroundWorker m_getStatusBW;
        public void Refresh()
        {
            if (Status != DualChangeGroupStatus.Complete)
            {
                if (m_getStatusBW == null)
                {
                    m_getStatusBW = new BackgroundWorker();
                    m_getStatusBW.DoWork += new DoWorkEventHandler(m_getStatusBW_DoWork);
                }

                if (!m_getStatusBW.IsBusy)
                {
                    m_getStatusBW.RunWorkerAsync();
                }
            }
        }

        private void m_getStatusBW_DoWork(object sender, DoWorkEventArgs e)
        {
            m_migrationInstructionsStatus = (ChangeStatus)(from cg in m_context.RTChangeGroupSet
                                                           where cg.Id == MigrationInstructions.Id
                                                           select cg.Status).FirstOrDefault();
            RaisePropertyChangedEvent("DisplayColor", null, null);
            RaisePropertyChangedEvent("Status", null, null);
        }

        private ChangeStatus m_deltaTablesStatus;
        private ChangeStatus m_migrationInstructionsStatus;
        private static readonly double s_alpha = .6;

        public string DisplayColor
        {
            get
            {
                Color displayColor;
                if (DeltaTables.ContainsBackloggedAction || MigrationInstructions.ContainsBackloggedAction)
                {
                    displayColor = Color.Red;
                }
                else
                {
                    switch (Status)
                    {
                        case DualChangeGroupStatus.Initialized:
                            displayColor = Color.Gray;
                            break;
                        case DualChangeGroupStatus.Pending:
                            displayColor = Color.LightBlue;
                            break;
                        case DualChangeGroupStatus.InProgress:
                            displayColor = Color.Yellow;
                            break;
                        case DualChangeGroupStatus.Complete:
                            displayColor = Color.Green;
                            break;
                        case DualChangeGroupStatus.Unknown:
                            displayColor = Color.Purple;
                            break;
                        default:
                            throw new ArgumentException("Invalid Status.");
                    }
                }
                if (!IsSelected)
                {
                    displayColor = Color.FromArgb((int)(255 * s_alpha), displayColor);
                }
                return "#" + displayColor.ToArgb().ToString("X");
            }
        }
        public DualChangeGroupStatus Status
        {
            get
            {
                ChangeStatus leftChangeStatus = m_deltaTablesStatus;
                ChangeStatus rightChangeStatus = m_migrationInstructionsStatus;

                switch (rightChangeStatus)
                {
                    case ChangeStatus.Pending:
                    case ChangeStatus.PendingConflictDetection:
                    case ChangeStatus.ChangeCreationInProgress:
                        return DualChangeGroupStatus.Pending;
                    case ChangeStatus.InProgress:
                        return DualChangeGroupStatus.InProgress;
                    case ChangeStatus.Complete:
                        return DualChangeGroupStatus.Complete;
                    default:
                        switch (leftChangeStatus)
                        {
                            case ChangeStatus.Delta:
                            case ChangeStatus.DeltaPending:
                                return DualChangeGroupStatus.Initialized;
                            default:
                                Console.WriteLine(leftChangeStatus + " " + rightChangeStatus);
                                return DualChangeGroupStatus.Unknown;
                        }
                }
            }
        }

        public string Name { get; private set; }

        public RTChangeGroup DeltaTables { get; private set; }

        public RTChangeGroup MigrationInstructions { get; private set; }

        public Guid SourceId
        {
            get
            {
                return DeltaTables.SourceUniqueId;
            }
        }

        public long Id
        {
            get
            {
                return MigrationInstructions.Id;
            }
        }

        public string Comment
        {
            get
            {
                return DeltaTables.Comment;
            }
        }

        public int ChangeActionCount { get; private set; }

        public double ChangeGroupHeight
        {
            get
            {
                return (Math.Log10(ChangeActionCount) * 10) + 1;
            }
        }
    }

    public enum ChangeGroupConflictStatus
    {
        None,
        Blocking,
        NonBlocking,
        Resolved
    }

    public enum DualChangeGroupStatus
    {
        Initialized,
        Pending,
        InProgress,
        Complete,
        Unknown
    }
}
