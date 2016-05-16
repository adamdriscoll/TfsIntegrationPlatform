// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement
{
    public static class UIHelper
    {
        public static void DisplayExtendedInformation(FrameworkElement element)
        {
            if (element != null)
            {
                ConflictRuleViewModel conflict = element.DataContext as ConflictRuleViewModel;
                if (conflict != null)
                {
                    try
                    {
                        ViewConflictDialog dialog = new ViewConflictDialog(conflict);
                        dialog.Owner = Window.GetWindow(element);
                        dialog.ShowDialog();
                    }
                    catch (Exception exception)
                    {
                        Utilities.HandleException(exception);
                    }
                }
            }
        }

        public static void AdjustLastColumnWidth(ListView listView)
        {
            if (listView != null)
            {
                double width = listView.ActualWidth;
                GridView gv = listView.View as GridView;
                for (int i = 0; i < gv.Columns.Count - 1; i++)
                {
                    if (!Double.IsNaN(gv.Columns[i].ActualWidth))
                        width -= gv.Columns[i].ActualWidth;
                }
                if (width > 50)
                {
                    Decorator border = VisualTreeHelper.GetChild(listView, 0) as Decorator;
                    ScrollViewer scroll = border.Child as ScrollViewer;
                    double d = scroll.ScrollableHeight;

                    if (d == 0)
                    {
                        gv.Columns.Last().Width = width - 10;
                    }
                    else
                    {
                        gv.Columns.Last().Width = width - SystemParameters.VerticalScrollBarWidth - 10;
                    }
                }
            }
        }

        private static GridViewColumnHeader _lastHeaderClicked = null;
        private static ListSortDirection _lastDirection = ListSortDirection.Ascending;

        public static void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            ListView listView = sender as ListView;
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (listView != null && headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    string header = headerClicked.Column.HeaderStringFormat;
                    Sort(header, direction, listView);

                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }

        }

        private static void Sort(string sortBy, ListSortDirection direction, ListView listView)
        {
            if (sortBy != null)
            {
                ICollectionView dataView =
                  CollectionViewSource.GetDefaultView(listView.ItemsSource);

                dataView.SortDescriptions.Clear();
                SortDescription sd = new SortDescription(sortBy, direction);
                dataView.SortDescriptions.Add(sd);
                dataView.Refresh();
            }
        }
    }
}
