// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.Windows.Controls;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    /// <summary>
    /// Interaction logic for ListPathsControl.xaml
    /// </summary>
    public partial class ListPathsControl : UserControl
    {
        public ListPathsControl()
        {
            InitializeComponent();
        }
    }

    public class ListPathsControlViewModel
    {
        public static readonly string PathForwardDelimiter = "/";
        public static readonly string PathBackwardDelimiter = "\\";
        public static readonly string PathSemicolonDelimiter = ";";

        public MigrationConflict Conflict { get; set;}
        public IList<string> AllPaths
        {
            get
            {
                // TODO - Adapter should provide GetParent method for scope
                List<string> paths = new List<string>();
                if (Conflict != null)
                {
                    string scopeHint = Conflict.ScopeHint;

                    string[] scopeSplit = scopeHint.Split(PathSemicolonDelimiter.ToCharArray());
                    string fullPath = scopeSplit[0];
                    if (fullPath.Contains(PathForwardDelimiter))
                    {
                        string[] splits = fullPath.Split(PathForwardDelimiter.ToCharArray());
                        paths.Add(string.Format("{0}{1}", splits[0], PathForwardDelimiter));
                        ProcessPaths(splits, splits[0], PathForwardDelimiter, paths);
                    }
                    else
                    {
                        string[] splits = fullPath.Split(PathBackwardDelimiter.ToCharArray());

                        string current = string.Empty;
                        paths.Add(PathBackwardDelimiter);
                        ProcessPaths(splits, string.Empty, PathBackwardDelimiter, paths);
                    }

                    //TODO: Remove this check in Sprint 7 
                    if (!(Conflict.ConflictType is VCContentConflictType && scopeSplit.Length > 1 &&
                        scopeSplit[1].Contains(PathForwardDelimiter)))
                    {
                        //Add complete path - Format Path;Changeset/Date(only if this is not VCContentConflictType)
                        paths.Add(Conflict.ScopeHint);
                    }
                    SelectedPath = paths[paths.Count-1];
                }
                return paths;
            }
        }

        private void ProcessPaths(string[] splits, string current, string delimiter, List<string> paths)
        {
            int i = 1;
            while (i < splits.Length)
            {
                current = string.Format("{0}{1}{2}", current, delimiter, splits[i++]);
                paths.Add(current);
            }
        }
        public string SelectedPath { get; set; }
    }
}
