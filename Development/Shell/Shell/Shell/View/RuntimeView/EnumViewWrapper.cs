// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    public static class EnumViewWrapper<T>
    {
        public static T Parse(string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }

        public static List<string> GetValues()
        {
            List<string> list = new List<string>();
            foreach (object value in Enum.GetValues(typeof(T)))
            {
                list.Add(value.ToString());
            }

            list.Sort(StringComparer.InvariantCultureIgnoreCase);
            return list;
        }

    }
}
