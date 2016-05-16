// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.BusinessModel.BusinessRuleEvaluation
{
    class DisallowAllNeglectedFilterResultItem : EvaluationResultItem
    {
        List<Session> m_invalidSessions = new List<Session>();
        const string Header = "Business rule evaluation of enforcing at least one non-neglected filter string pair per session: {0}\n";

        internal override void Print(StringBuilder sb)
        {
            if (Passed)
            {
                sb.AppendFormat(Header, "Passed");
            }
            else
            {
                sb.AppendFormat(Header, "Failed");
                foreach (var session in m_invalidSessions)
                {
                    sb.AppendLine(Indent + string.Format(
                        "Invalid session: {0} (Unique Id: {1})", 
                        session.FriendlyName ?? string.Empty,
                        new Guid(session.SessionUniqueId).ToString()));
                }
                sb.AppendLine();
            }
        }

        internal void AddInvalidSession(Session session)
        {
            Passed = false;
            if (!m_invalidSessions.Contains(session))
            {
                m_invalidSessions.Add(session);
            }
        }
    }

    class DisallowAllNeglectedFilterRule : IEvaluationRule
    {

        #region IEvaluationRule Members

        public EvaluationResultItem Evaluate(Configuration configuration)
        {
            DisallowAllNeglectedFilterResultItem resultItem = new DisallowAllNeglectedFilterResultItem();

            foreach (Session session in configuration.SessionGroup.Sessions.Session)
            {
                PerSessionValidation(resultItem, session);
            }

            return resultItem;
        }

        private void PerSessionValidation(
            DisallowAllNeglectedFilterResultItem resultItem, 
            Session session)
        {
            bool hasNonNeglectFilter = false;
            foreach (var filterPair in session.Filters.FilterPair)
            {
                if (!filterPair.Neglect)
                {
                    hasNonNeglectFilter = true;
                    break;
                }
            }

            if (!hasNonNeglectFilter)
            {
                resultItem.AddInvalidSession(session);
            }
        }

        #endregion
    }
}
