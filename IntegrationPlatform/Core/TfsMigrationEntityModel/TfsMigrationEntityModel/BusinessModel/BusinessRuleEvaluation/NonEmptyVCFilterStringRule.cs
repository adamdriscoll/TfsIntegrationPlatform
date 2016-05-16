// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Microsoft.TeamFoundation.Migration.BusinessModel.BusinessRuleEvaluation
{
    class NonEmptyVCFilterStringResultItem : EvaluationResultItem
    {
        List<Session> m_invalidSessions = new List<Session>();
        const string Header = "Version Control session non-empty filter string business rule evaluation: {0}\n";

        internal override void Print(StringBuilder sb)
        {
            if (Passed)
            {
                sb.AppendFormat(Header, "Passed");
            }
            else
            {
                sb.AppendFormat(Header, "Failed");
                foreach (Session s in m_invalidSessions)
                {
                    sb.AppendLine(Indent + string.Format("Session: {0} ({1})", s.FriendlyName, new Guid(s.SessionUniqueId).ToString()));
                }
                sb.AppendLine();
            }
        }

        internal void AddInvalidSession(Session session)
        {
            if (!m_invalidSessions.Contains(session))
            {
                m_invalidSessions.Add(session);
            }
        }
    }

    class NonEmptyVCFilterStringRule : IEvaluationRule
    {
        #region IEvaluationRule Members

        public EvaluationResultItem Evaluate(Configuration configuration)
        {
            NonEmptyVCFilterStringResultItem resultItem = new NonEmptyVCFilterStringResultItem();
            resultItem.Passed = true;

            foreach (Session session in configuration.SessionGroup.Sessions.Session)
            {
                bool isValid = true;
                string settingXml = BusinessModelManager.GenericSettingXmlToString(session.CustomSettings.SettingXml);
                if (!string.IsNullOrEmpty(settingXml))
                {
                    XmlDocument settingDoc = new XmlDocument();
                    settingDoc.LoadXml(settingXml);

                    if (session.SessionType != SessionTypeEnum.VersionControl)
                    {
                        continue;
                    }

                    foreach (FilterPair pair in session.Filters.FilterPair)
                    {
                        if (!isValid) break;

                        foreach (FilterItem item in pair.FilterItem)
                        {
                            if (string.IsNullOrEmpty(item.FilterString))
                            {
                                resultItem.Passed = false;
                                resultItem.AddInvalidSession(session);
                                isValid = false;
                                break;
                            }
                        }
                    }
                }
            }

            return resultItem;
        }

        #endregion
    }
}
