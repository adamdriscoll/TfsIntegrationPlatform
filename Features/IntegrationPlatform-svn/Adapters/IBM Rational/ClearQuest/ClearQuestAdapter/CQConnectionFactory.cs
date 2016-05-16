// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    public static class CQConnectionFactory
    {
        private static Dictionary<string, Session> s_userSessions = new Dictionary<string,Session>();
        private static object s_userSessionLock = new object();

        private static Dictionary<string, AdminSession> s_adminSessions = new Dictionary<string, AdminSession>();
        private static object s_adminSessionLock = new object();

        public static Session GetUserSession(ClearQuestConnectionConfig connConfig)
        {
            string idStr = GenerateSessionIdStr(connConfig);

            lock (s_userSessionLock)
            {
                if (s_userSessions.ContainsKey(idStr))
                {
                    return s_userSessions[idStr];
                }

                // create and initialize session object
                Session userSession = CQWrapper.CreateSession();
                TraceManager.TraceInformation("Connecting to CQ User Session");
                CQWrapper.UserLogon(userSession,
                                    connConfig.User,
                                    connConfig.Password,
                                    connConfig.UserDB,
                                    (int)CQConstants.SessionType.PRIVATE,
                                    connConfig.DBSet);
                TraceManager.TraceInformation("Connected to CQ User Session");

                s_userSessions.Add(idStr, userSession);

                return userSession;
            }
        }

        public static AdminSession GetAdminSession(ClearQuestConnectionConfig connConfig)
        {
            string idStr = GenerateSessionIdStr(connConfig);

            lock (s_adminSessionLock)
            {
                if (s_adminSessions.ContainsKey(idStr))
                {
                    return s_adminSessions[idStr];
                }

                // create and initialize admin session object
                AdminSession adminSession = CQWrapper.CreateAdminSession();
                TraceManager.TraceInformation("Connecting to CQ Admin Session");
                CQWrapper.AdminLogon(adminSession,
                                     connConfig.User,
                                     connConfig.Password,
                                     connConfig.DBSet);
                TraceManager.TraceInformation("Connected to CQ Admin Session");
                OAdUser cqUser = CQWrapper.GetUser(adminSession, connConfig.User);
                try
                {
                    if (!CQWrapper.IsSuperUser(cqUser))
                    {
                        string errMsg = UtilityMethods.Format(CQResource.CQ_NO_ADMIN_RIGHT,
                                                              connConfig.User ?? string.Empty);
                        TraceManager.TraceError(errMsg);
                        throw new MigrationException(errMsg);
                    }
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    string errMsg = UtilityMethods.Format(CQResource.CQ_NO_ADMIN_RIGHT,
                                                          connConfig.User ?? string.Empty);
                    TraceManager.TraceException(ex);
                    TraceManager.TraceError(errMsg);
                    throw new MigrationException(errMsg);
                }

                s_adminSessions.Add(idStr, adminSession);

                return adminSession;
            }
        }

        private static string GenerateSessionIdStr(ClearQuestConnectionConfig connConfig)
        {
            return connConfig.DBSet + connConfig.UserDB + connConfig.User;
        }
    }
}
