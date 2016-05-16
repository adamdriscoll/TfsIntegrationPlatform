// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal static class CQUtilityMethods
    {
        public static string[] FindAllChangeActionNamesByType(
            Session userSession, 
            OAdEntity entity,
            int changeType)
        {
            // find the entity type
            OAdEntityDef entityDef = CQWrapper.GetEntityDef(userSession, CQWrapper.GetEntityDefName(entity));

            // find the MODIFY action def name to open the record
            object[] actionDefNames = CQWrapper.GetActionDefNames(entityDef) as object[];
            string[] allValidActionNames = CQUtilityMethods.FindActionNameByType(entityDef, actionDefNames, changeType);

            return allValidActionNames;
        }

        public static string[] FindActionNameByType(
            OAdEntityDef entityDef,
            object[] transitions,
            int matchingActionDefType)
        {
            List<string> matchingActionDefNames = new List<string>(transitions.Count());

            foreach (object actionNameObj in transitions)
            {
                string actionDefName = actionNameObj as string;
                Debug.Assert(!string.IsNullOrEmpty(actionDefName), "string.IsNullOrEmpty(actionName)");

                int actionDefType = CQWrapper.GetActionDefType(entityDef, actionDefName);

                if (actionDefType == matchingActionDefType)
                {
                    matchingActionDefNames.Add(actionDefName);
                }
            }

            return matchingActionDefNames.ToArray();
        }

        public static string[] FindAllActionNameByTypeAndSourceState(
            OAdEntityDef entityDef,
            string srcState,
            int matchingActionDefType)
        {
            List<string> retVal = new List<string>();

            // find all possible destination state
            object[] allStateObjs = CQWrapper.GetStateDefNames(entityDef) as object[];

            if (null != allStateObjs)
            {
                foreach (object destStateObj in allStateObjs)
                {
                    string destState = destStateObj as string;
                    if (!string.IsNullOrEmpty(destState))
                    {
                        object[] transitions = CQWrapper.DoesTransitionExist(entityDef, srcState, destState) as object[];
                        string[] actionDefNames = FindActionNameByType(entityDef, transitions, matchingActionDefType);
                        retVal.AddRange(actionDefNames.AsEnumerable());
                    }
                }
            }

            return retVal.ToArray();
        }

        public static string[] FindAllActionNameByTypeAndStateTransition(
            OAdEntityDef entityDef,
            string srcState,
            string destState,
            int matchingActionDefType)
        {
            if (string.IsNullOrEmpty(srcState))
            {
                throw new ArgumentNullException("srcState");
            }

            if (string.IsNullOrEmpty(destState))
            {
                throw new ArgumentNullException("destState");
            }

            List<string> retVal = new List<string>();

            object[] transitions = CQWrapper.DoesTransitionExist(entityDef, srcState, destState) as object[];
            string[] actionDefNames = FindActionNameByType(entityDef, transitions, matchingActionDefType);
            retVal.AddRange(actionDefNames.AsEnumerable());            

            return retVal.ToArray();
        }

        public static DateTime GetTimeForNewHighWaterMark(string cqTimeOffsetFromServerHistoryTimesInMinutes)
        {
            DateTime newHwmValue;
            if (string.IsNullOrEmpty(cqTimeOffsetFromServerHistoryTimesInMinutes))
            {
                newHwmValue = DateTime.UtcNow;
            }
            else
            {
                int timeOffsetInMinutes;
                if (int.TryParse(cqTimeOffsetFromServerHistoryTimesInMinutes, out timeOffsetInMinutes))
                {
                    // Adjust to the time zone used on the server based on the configuration.
                    newHwmValue = DateTime.Now + new TimeSpan(0, 0-timeOffsetInMinutes, 0);
                }
                else
                {
                    TraceManager.TraceWarning(
                        "The configured value for CQTimeOffsetFromServerHistoryTimesInMinutes is not an integer: '{0}'; UTC time will be used for the HighWaterMark",
                        cqTimeOffsetFromServerHistoryTimesInMinutes);
                    newHwmValue = DateTime.UtcNow;
                }
            }
            return newHwmValue;
        }

        public static DateTime TryParseCQDate(string d)
        {
            if (string.IsNullOrEmpty(d))
                throw new ArgumentNullException("d");

            try
            {
                return DateTime.Parse(d, CultureInfo.CurrentCulture);
            }
            catch (FormatException ex)
            {
                // test for the DB2 TIMESTAMP pattern
                var pattern = "(?<date>\\d{4}-\\d{2}-\\d{2})-(?<hour>\\d{2}).(?<minute>\\d{2}).(?<second>\\d{2}.\\d{6})";
                // fix it
                if (Regex.IsMatch(d, pattern))
                {
                    var s = Regex.Replace(d, pattern, "${date} ${hour}:${minute}:${second}");
                    try
                    {
                        return DateTime.Parse(s, CultureInfo.CurrentCulture);
                    }
                    catch (Exception ex2)
                    {
                        throw new Exception("Tried special DB2 timestamp parser, but it failed too", ex2);
                    }
                }

                throw ex;
            }
        }
    }
}
