// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Windows.Data;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement
{
    public class IsResolvedConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is ResolvedStatus)
            {
                ResolvedStatus status = (ResolvedStatus)value;
                switch (status)
                {
                    case ResolvedStatus.Resolved:
                        return "Gray";
                    case ResolvedStatus.Failed:
                    case ResolvedStatus.Unresolved:
                        return "Black";
                    default:
                        throw new Exception("Enum not recognized.");
                }
            }
            else
            {
                throw new Exception("Wrong input in converter.");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class ResolvedStatusConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is ResolvedStatus)
            {
                ResolvedStatus status = (ResolvedStatus)value;
                switch (status)
                {
                    case ResolvedStatus.Resolved:
                        return "../Resources/Images/successfulresolve.png";
                    case ResolvedStatus.Failed:
                        return "../Resources/Images/failedresolve.png";
                    case ResolvedStatus.Unresolved:
                        return "../Resources/Images/unresolved.png";
                    default:
                        throw new Exception("Enum not recognized.");
                }
            }
            else
            {
                throw new Exception("Wrong input in converter.");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class IsResolvableConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                return (bool)value ? "Yellow" : "White";
            }
            else
            {
                throw new Exception("Error");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
