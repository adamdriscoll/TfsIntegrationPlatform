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

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Interaction logic for CustomSettingsView.xaml
    /// </summary>
    public partial class CustomSettingsView : UserControl
    {
        public CustomSettingsView()
        {
            InitializeComponent();
        }

        private void editXmlButton_Click(object sender, RoutedEventArgs e)
        {
            SerializableElement element = (sender as Button).DataContext as SerializableElement;
            element.IsEditingXml = true;

            string cachedSerialization = element.SerializedContent;

            EditXmlDialog dialog = new EditXmlDialog(element);
            dialog.Owner = Window.GetWindow(this);
            bool result = (bool)dialog.ShowDialog();

            if (!result)
            {
                element.SerializedContent = cachedSerialization;
            }

            element.IsEditingXml = false;
        }
    }

    public class CustomSettingsConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (new SerializableCustomSettings(value as Session));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class FieldMapsDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is BasicFieldMapViewModel)
            {
                BasicFieldMapViewModel fieldMap = item as BasicFieldMapViewModel;
                if (fieldMap.Values == null)
                {
                    return FindResource(container, "fieldMapWithoutValueMap");
                }
                else
                {
                    return FindResource(container, "fieldMapWithValueMap");
                }
            }
            else
            {
                return FindResource(container, "fieldMapWithValueMap");
            }
        }

        public DataTemplate FindResource(DependencyObject container, string key)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(container);
            while (!(parent is UserControl))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return (parent as UserControl).FindResource(key) as DataTemplate;
        }
    }
}
