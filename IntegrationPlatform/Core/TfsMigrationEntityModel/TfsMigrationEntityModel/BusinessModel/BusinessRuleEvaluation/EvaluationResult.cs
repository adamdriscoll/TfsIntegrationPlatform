// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.BusinessModel.BusinessRuleEvaluation
{
    /// <summary>
    /// The collection of result items that describes the evaluation result of a configuration.
    /// </summary>
    public class EvaluationResult
    {
        const string PassedResultMessage = "The configuration has passed all the business rule evaluation.";
        const string FailedResultMessage = "The configuration has failed the business rule evaluation.";

        List<EvaluationResultItem> m_evaluationResultItems = new List<EvaluationResultItem>();

        /// <summary>
        /// Gets whether all the rule evaluation passes or nottttt
        /// </summary>
        public bool Passed
        {
            get
            {
                return (0 == m_evaluationResultItems.Count);
            }
        }

        /// <summary>
        /// Gets a collection of result items
        /// </summary>
        public ReadOnlyCollection<EvaluationResultItem> ResultItems
        {
            get
            {
                return m_evaluationResultItems.AsReadOnly();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (Passed)
            {
                sb.AppendLine(PassedResultMessage);
            }
            else
            {
                sb.AppendLine(FailedResultMessage);

                foreach (var item in m_evaluationResultItems)
                {
                    item.Print(sb);
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        internal void AddResultItem(EvaluationResultItem item)
        {
            m_evaluationResultItems.Add(item);
        }
    }
}
