// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.BusinessModel.BusinessRuleEvaluation
{
    /// <summary>
    /// An item that describes the evaluation result of one business rule
    /// </summary>
    public abstract class EvaluationResultItem
    {
        protected const string Indent = "  ";

        /// <summary>
        /// Constructor
        /// </summary>
        public EvaluationResultItem()
        {
            Passed = true;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="passed"></param>
        public EvaluationResultItem(bool passed)
        {
            Passed = passed;
        }

        /// <summary>
        /// Gets whether the evaluation passed or not
        /// </summary>
        public bool Passed { get; internal set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            Print(sb);
            return sb.ToString();
        }

        /// <summary>
        /// Print the detailed result
        /// </summary>
        /// <param name="sb"></param>
        internal abstract void Print(StringBuilder sb);
    }
}
