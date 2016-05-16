// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Globalization;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter
{
    internal class TfsWITQueryBuilder
    {
        /// <summary>
        /// Return a Wiql query based on a column list, condition, and optional Order By clause 
        /// </summary>
        /// <param name="columnList">A comma separated list of column names to be used in the Wiql query</param>
        /// <param name="condition">A condition string that is valid in the WHERE clause of a Wiql query</param>
        /// <param name="orderByClause">A clause that is value in the ORDER BY clause of a Wiql query; this can be null or empty if order is not required.</param>
        /// <returns></returns>
        internal static string BuildWiqlQuery(string columnList, string condition, string orderByClause)
        {
            StringBuilder sb = new StringBuilder(
                string.Format(CultureInfo.InvariantCulture, "SELECT {0} FROM WorkItems WHERE {1}", columnList, condition));
            if (!string.IsNullOrEmpty(orderByClause))
            {
                sb.Append(" ORDER BY ");
                sb.Append(orderByClause);
            }
            return sb.ToString();
        }
    }

}
