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
    /// The engine that drives the evaluation of various business rules against a configuration file.
    /// </summary>
    class BusinessRuleEvaluationEngine
    {
        private List<IEvaluationRule> m_evaluationRules = new List<IEvaluationRule>();

        /// <summary>
        /// Constructor
        /// </summary>
        public BusinessRuleEvaluationEngine()
        {
            AddRule(new NonEmptyVCFilterStringRule());
            AddRule(new NoMultipleMapsOnSameTargetFieldRule());
            AddRule(new AggregatedSourceFieldIndexRule());
            AddRule(new DisallowAllNeglectedFilterRule());
            AddRule(new DisallowSameSourceFieldMappedTwiceInMappedFieldsRule());
        }

        /// <summary>
        /// Conducts business rule evalution on a configuration
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns>A collection of evaluation result, each of which describes a failed rule evalution; empty collection if the evaluation of all the rules passes</returns>
        public EvaluationResult Evaluate(Configuration configuration)
        {
            EvaluationResult evaluationResult = new EvaluationResult();

            foreach (IEvaluationRule rule in m_evaluationRules)
            {
                EvaluationResultItem result = rule.Evaluate(configuration);
                if (!result.Passed)
                {
                    evaluationResult.AddResultItem(result);
                }
            }

            return evaluationResult;
        }

        private void AddRule(IEvaluationRule evaluationRule)
        {
            m_evaluationRules.Add(evaluationRule);
        }
    }
}
