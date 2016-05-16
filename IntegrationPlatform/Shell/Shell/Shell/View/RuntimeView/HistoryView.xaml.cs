// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.TeamFoundation.Migration.Shell.ViewModel;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    /// <summary>
    /// Interaction logic for HistoryView.xaml
    /// </summary>
    public partial class HistoryView : UserControl
    {
        private DispatcherTimer m_timer;
        private HistoryViewModel m_historyViewModel;
        private int m_width;
        private double m_preferredOffset;
        private FrameworkElement m_selectedChangeGroup;
        private double m_selectedChangeGroupOffsetToViewport;
        
        public HistoryView()
        {
            InitializeComponent();
            m_timer = new DispatcherTimer();
            m_timer.Interval = TimeSpan.FromSeconds(1);
            m_timer.Tick += new EventHandler(m_timer_Tick);
        }

        private void m_timer_Tick(object sender, EventArgs e)
        {
            m_historyViewModel.SelectedOneWaySession.Width = m_width;
            m_historyViewModel.SelectedOneWaySession.Refresh();
            m_timer.Stop();
        }

        private int m_tempWidth;
        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                m_tempWidth = (int)e.NewSize.Width;
                if (m_historyViewModel != null)
                {
                    m_historyViewModel.Width = (int)e.NewSize.Width;
                    if ((m_historyViewModel.SelectedOneWaySession.Width < (int)e.NewSize.Width || m_historyViewModel.SelectedOneWaySession.Width < m_width))
                    {
                        m_width = (int)e.NewSize.Width;
                        m_timer.Stop();
                        if (m_historyViewModel.SelectedOneWaySession.Width < m_width)
                        {
                            m_timer.Start();
                        }
                    }
                }
            }
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            m_historyViewModel = (sender as FrameworkElement).DataContext as HistoryViewModel;
            if (m_historyViewModel != null)
            {
                m_historyViewModel.Width = m_tempWidth;
                if (m_historyViewModel.SelectedOneWaySession != null)
                {
                    m_historyViewModel.SelectedOneWaySession.Width = m_tempWidth;
                    m_historyViewModel.SelectedOneWaySession.Refresh();
                }
            }
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_historyViewModel != null)
            {
                m_preferredOffset = 0;
                m_historyViewModel.SelectedOneWaySession.GetNext();
            }
        }

        private void previousButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_historyViewModel != null)
            {
                m_preferredOffset = changeGroupsScrollViewer.ExtentWidth - changeGroupsScrollViewer.HorizontalOffset;
                m_historyViewModel.SelectedOneWaySession.GetPrevious();
            }
        }

        private void onMouseEnter(object sender, RoutedEventArgs e)
        {
            m_selectedChangeGroup = sender as FrameworkElement;
            (m_selectedChangeGroup.DataContext as DualChangeGroupViewModel).IsSelected = true;

            m_selectedChangeGroupOffsetToViewport = (m_selectedChangeGroup.TransformToAncestor(VisualTreeHelper.GetParent(changeGroupsScrollViewer) as Visual) as MatrixTransform).Matrix.OffsetX;
        }

        private void onMouseLeave(object sender, RoutedEventArgs e)
        {
            ((sender as FrameworkElement).DataContext as DualChangeGroupViewModel).IsSelected = false;
        }

        private void zoomScrollViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Delta > 0)
                {
                    changeGroupsScrollViewer.Zoom++;
                }
                else if (e.Delta < 0)
                {
                    changeGroupsScrollViewer.Zoom--;
                }
            }
            else
            {
                if (e.Delta > 0)
                {
                    changeGroupsScrollViewer.ScrollToHorizontalOffset(changeGroupsScrollViewer.HorizontalOffset - 16);
                }
                else if (e.Delta < 0)
                {
                    changeGroupsScrollViewer.ScrollToHorizontalOffset(changeGroupsScrollViewer.HorizontalOffset + 16);
                }
            }
        }

        private void changeGroupsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentWidthChange != 0)
            {
                if (m_selectedChangeGroup != null)
                {
                    double selectedChangeGroupOffsetToParent = (m_selectedChangeGroup.TransformToAncestor(VisualTreeHelper.GetParent(changeGroupsItemsControl) as Visual) as MatrixTransform).Matrix.OffsetX;
                    changeGroupsScrollViewer.ScrollToHorizontalOffset((selectedChangeGroupOffsetToParent - m_selectedChangeGroupOffsetToViewport));
                }
                else if (m_preferredOffset != 0)
                {
                    changeGroupsScrollViewer.ScrollToHorizontalOffset(e.ExtentWidth - m_preferredOffset);
                }
            }
        }
        
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            m_selectedChangeGroup = null;
        }

        private void refreshButton_Click(object sender, RoutedEventArgs e)
        {
            m_historyViewModel.SelectedOneWaySession.Refresh();
        }
    }

    public class ZoomScrollViewer : ScrollViewer
    {
        private static int s_defaultMinZoom = 0;
        private static int s_defaultMaxZoom = 10;
        private static int s_defaultZoom = 6;

        public static readonly DependencyProperty MinZoomProperty = DependencyProperty.Register("MinZoom", typeof(int), typeof(ZoomScrollViewer), new UIPropertyMetadata(s_defaultMinZoom));
        public static readonly DependencyProperty MaxZoomProperty = DependencyProperty.Register("MaxZoom", typeof(int), typeof(ZoomScrollViewer), new UIPropertyMetadata(s_defaultMaxZoom));
        public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register("Zoom", typeof(int), typeof(ZoomScrollViewer), new UIPropertyMetadata(s_defaultZoom, new PropertyChangedCallback(OnZoomChanged), new CoerceValueCallback(CoerceZoom)));
        public static RoutedEvent ZoomChangedEvent = EventManager.RegisterRoutedEvent("ZoomChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ZoomScrollViewer));

        public event RoutedEventHandler ZoomChanged
        {
            add
            {
                AddHandler(ZoomChangedEvent, value);
            }
            remove
            {
                RemoveHandler(ZoomChangedEvent, value);
            }
        }

        public int MinZoom
        {
            get
            {
                return (int)GetValue(MinZoomProperty);
            }
            set
            {
                SetValue(MinZoomProperty, value);
                CoerceValue(ZoomProperty);
            }
        }

        public int MaxZoom
        {
            get
            {
                return (int)GetValue(MaxZoomProperty);
            }
            set
            {
                SetValue(MaxZoomProperty, value);
                CoerceValue(ZoomProperty);
            }
        }

        public int Zoom
        {
            get
            {
                return (int)GetValue(ZoomProperty);
            }
            set
            {
                SetValue(ZoomProperty, value);
            }
        }

        private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement element = (UIElement)d;
            RoutedEventArgs args = new RoutedEventArgs(ZoomScrollViewer.ZoomChangedEvent, d);
            element.RaiseEvent(args);
        }

        private static object CoerceZoom(DependencyObject d, object value)
        {
            ZoomScrollViewer zoomScrollViewer = d as ZoomScrollViewer;

            int zoom = (int)value;
            if (zoom < zoomScrollViewer.MinZoom)
            {
                zoom = zoomScrollViewer.MinZoom;
            }
            else if (zoom > zoomScrollViewer.MaxZoom)
            {
                zoom = zoomScrollViewer.MaxZoom;
            }
            return zoom;
        }
    }
}
