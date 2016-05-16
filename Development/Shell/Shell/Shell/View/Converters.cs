// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Windows.Data;
using Microsoft.TeamFoundation.Migration.Shell.ViewModel;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    class StatusConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            DualChangeGroupStatus status = (DualChangeGroupStatus)Enum.Parse(typeof(DualChangeGroupStatus), value.ToString());
            switch (status)
            {
                case DualChangeGroupStatus.Initialized:
                    return "Gray";
                case DualChangeGroupStatus.Pending:
                    return "LightBlue";
                case DualChangeGroupStatus.InProgress:
                    return "Yellow";
                case DualChangeGroupStatus.Complete:
                    return "Green";
                case DualChangeGroupStatus.Unknown:
                    return "Red";
                default:
                    throw new ArgumentException("Invalid Status.");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
