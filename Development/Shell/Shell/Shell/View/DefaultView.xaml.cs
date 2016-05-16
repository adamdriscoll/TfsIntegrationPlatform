// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Windows.Controls;
using Microsoft.TeamFoundation.Migration.Shell.View;
using System;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    /// <summary>
    /// Interaction logic for DefaultView.xaml
    /// </summary>
    public partial class DefaultView : UserControl
    {
        public DefaultView()
        {
            InitializeComponent();
        }
    }
    public class MigrationViewConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            MigrationStatusViews view = (MigrationStatusViews)value;
            Visibility result = Visibility.Collapsed;
            string param = parameter as string;
            if ((param.Equals("configuration") && view == MigrationStatusViews.Configuration) ||
                (param.Equals("progress") && view == MigrationStatusViews.Progress) ||
                (param.Equals("conflicts") && view == MigrationStatusViews.Conflicts))
                {
                    result = Visibility.Visible;
                }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

}
