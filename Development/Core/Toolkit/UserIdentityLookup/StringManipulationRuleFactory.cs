// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    static class StringManipulationRuleFactory
    {
        static SimpleTextReplacementRule s_simpleTextReplacementRule = new SimpleTextReplacementRule();
        static IgnoreTextRule s_ignoreTextRule = new IgnoreTextRule();
        static FormatStringCompositionRule s_formatStringCompositionRule = new FormatStringCompositionRule();
        static FormatStringDecompositionRule s_formatStringDecompositionRule = new FormatStringDecompositionRule();

        public static IStringManipulationRule GetInstance(MappingRules mappingRuleType)
        {
            switch (mappingRuleType)
            {
                case MappingRules.SimpleReplacement:
                    return s_simpleTextReplacementRule;
                case MappingRules.Ignore:
                    return s_ignoreTextRule;
                case MappingRules.FormatStringComposition:
                    return s_formatStringCompositionRule;
                case MappingRules.FormatStringDecomposition:
                    return s_formatStringDecompositionRule;
                default:
                    Debug.Assert(false, "Unknown DisplayNameMapping.MappingRule type");
                    TraceManager.TraceError("Unknown DisplayNameMapping.MappingRule type");
                    return null;
            }
        }
    }
}
