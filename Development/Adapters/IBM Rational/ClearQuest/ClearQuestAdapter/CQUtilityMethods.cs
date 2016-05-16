// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ClearQuestOleServer;
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
    }
}
