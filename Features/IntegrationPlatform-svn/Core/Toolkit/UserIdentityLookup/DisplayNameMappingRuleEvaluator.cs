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
    class DisplayNameMappingRuleEvaluator : IUserIdMappingRuleEvaluator
    {
        Dictionary<MappingDirectionEnum, NotifyingCollection<DisplayNameMapping>> m_perDirectionMappings
            = new Dictionary<MappingDirectionEnum, NotifyingCollection<DisplayNameMapping>>();

        DisplayNameMappingComparer m_mappingRuleComparer = new DisplayNameMappingComparer();

        public DisplayNameMappingRuleEvaluator(
            NotifyingCollection<DisplayNameMappings> displayNameMappings)
        {
            Initialize(displayNameMappings);
        }

        
        #region IIdentityLookupAlgorithm Members

        /// <summary>
        /// Try map the user identity based on the mappings rules in the configuration
        /// </summary>
        /// <param name="sourceUserIdentity"></param>
        /// <param name="context"></param>
        /// <param name="mappedUserIdentity"></param>
        /// <returns>True if a mapping rule is applied to the sourceUserIdentity; FALSE otherwise</returns>
        public bool TryMapUserIdentity(
            RichIdentity sourceUserIdentity, 
            IdentityLookupContext context, 
            RichIdentity mappedUserIdentity)
        {
            switch (context.MappingDirection)
            {
                case MappingDirectionEnum.LeftToRight:
                    return MapUser(sourceUserIdentity, true, mappedUserIdentity);
                case MappingDirectionEnum.RightToLeft:
                    return MapUser(sourceUserIdentity, false, mappedUserIdentity);
                default:
                    TraceManager.TraceError("Unknown context.MappingDirection: {0}", context.MappingDirection.ToString());
                    return false;
            }
        }

        #endregion

        private void Initialize(
            NotifyingCollection<DisplayNameMappings> displayNameMappings)
        {
            foreach (DisplayNameMappings mappingCollection in displayNameMappings)
            {
                if (!m_perDirectionMappings.ContainsKey(mappingCollection.DirectionOfMapping))
                {
                    m_perDirectionMappings.Add(mappingCollection.DirectionOfMapping, new NotifyingCollection<DisplayNameMapping>());
                }

                m_perDirectionMappings[mappingCollection.DirectionOfMapping].AddRange(mappingCollection.DisplayNameMapping.ToArray());
            }
        }

        private bool MapUser(
            RichIdentity sourceUserIdentity,
            bool leftToRight,
            RichIdentity mappedUserIdentity)
        {
            m_mappingRuleComparer.MapFromLeftToRight = leftToRight;

            NotifyingCollection<DisplayNameMapping> unidirectionalRules;
            if (leftToRight)
            {
                if (!m_perDirectionMappings.ContainsKey(MappingDirectionEnum.LeftToRight))
                {
                    return false;
                }
                unidirectionalRules = m_perDirectionMappings[MappingDirectionEnum.LeftToRight];
            }
            else
            {
                if (!m_perDirectionMappings.ContainsKey(MappingDirectionEnum.RightToLeft))
                {
                    return false;
                }
                unidirectionalRules = m_perDirectionMappings[MappingDirectionEnum.RightToLeft];
            }

            bool mapped = false;

            DisplayNameMapping appliedUnidirectionalRule = null;
            RichIdentity unidirectionalMappingOutput = null;
            foreach (DisplayNameMapping rule in unidirectionalRules)
            {
                RichIdentity mappingOutput = new RichIdentity();
                if (TryApplyMappingRule(sourceUserIdentity, leftToRight, mappingOutput, rule))
                {
                    if (appliedUnidirectionalRule == null
                        || m_mappingRuleComparer.Compare(rule, appliedUnidirectionalRule) < 0)
                    {
                        appliedUnidirectionalRule = rule;
                        unidirectionalMappingOutput = mappingOutput;
                    }
                    mapped = true;
                }
            }

            if (mapped)
            {
                mappedUserIdentity.DisplayName = unidirectionalMappingOutput.DisplayName;
                return true;
            }

            DisplayNameMapping appliedBidirectionalRule = null;
            RichIdentity bidirectionalMappingOutput = null;
            if (m_perDirectionMappings.ContainsKey(MappingDirectionEnum.TwoWay))
            {
                foreach (DisplayNameMapping rule in m_perDirectionMappings[MappingDirectionEnum.TwoWay])
                {
                    RichIdentity mappingOutput = new RichIdentity();
                    if (TryApplyMappingRule(sourceUserIdentity, leftToRight, mappingOutput, rule))
                    {
                        if (appliedBidirectionalRule == null
                            || m_mappingRuleComparer.Compare(rule, appliedBidirectionalRule) < 0)
                        {
                            appliedBidirectionalRule = rule;
                            bidirectionalMappingOutput = mappingOutput;
                        }
                        mapped = true;
                    }
                }
            }

            if (mapped)
            {
                mappedUserIdentity.DisplayName = bidirectionalMappingOutput.DisplayName;
            }

            return mapped;
        }

        private bool TryApplyMappingRule(
            RichIdentity sourceUserIdentity, 
            bool leftToRight, 
            RichIdentity mappingOutput, 
            DisplayNameMapping rule)
        {
            if (IsRuleApplicable(sourceUserIdentity, rule, leftToRight))
            {
                return ApplyRule(sourceUserIdentity, leftToRight, mappingOutput, rule);
            }
            else
            {
                return false;
            }
        }

        private bool ApplyRule(RichIdentity sourceUserIdentity, bool leftToRight, RichIdentity mappingOutput, DisplayNameMapping rule)
        {
            string toDisplayName = (leftToRight ? rule.Right : rule.Left);
            IStringManipulationRule stringManipulationRule = StringManipulationRuleFactory.GetInstance(rule.MappingRule);

            if (stringManipulationRule != null)
            {
                mappingOutput.DisplayName = stringManipulationRule.Apply(sourceUserIdentity.DisplayName, toDisplayName);
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsRuleApplicable(RichIdentity sourceUserIdentity, DisplayNameMapping rule, bool leftToRight)
        {
            string fromDispName = (leftToRight ? rule.Left : rule.Right);

            if (string.IsNullOrEmpty(fromDispName))
            {
                return false;
            }

            bool mappingRuleIsKnown = false;
            switch (rule.MappingRule)
            {
                case MappingRules.SimpleReplacement:
                case MappingRules.Ignore:
                case MappingRules.FormatStringComposition:
                case MappingRules.FormatStringDecomposition:
                    mappingRuleIsKnown = true;
                    break;
                default:
                    TraceManager.TraceError("Unknown DisplayNameMapping.MappingRule type");
                    mappingRuleIsKnown = false;
                    break;
            }

            if (!mappingRuleIsKnown)
            {
                return false;
            }

            return (fromDispName.Equals(UserIdentityMappingConfigSymbols.ANY, StringComparison.OrdinalIgnoreCase)
                   || fromDispName.Equals(sourceUserIdentity.DisplayName ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        }

        class DisplayNameMappingComparer : IComparer<DisplayNameMapping>
        {
            public bool MapFromLeftToRight { get; set; }

            #region IComparer<DisplayNameMapping> Members

            public int Compare(DisplayNameMapping x, DisplayNameMapping y)
            {
                // return -1 if x is the more specific rule
                // return 1 if x is the less specific rule

                string xFromDisplayName = (MapFromLeftToRight ? x.Left : x.Right);
                string yFromDisplayName = (MapFromLeftToRight ? y.Left : y.Right);

                if (string.IsNullOrEmpty(xFromDisplayName) || string.IsNullOrEmpty(yFromDisplayName))
                {
                    // not comparable, assume to be equivalent
                    return 0;
                }

                if (AttributeContainsWildcard(xFromDisplayName))
                {
                    if (AttributeContainsWildcard(yFromDisplayName))
                    {
                        return 0;
                    }
                    else
                    {
                        // x is less specific
                        return 1;
                    }
                }
                else // !AttributeContainsWildcard(xFromDisplayName)
                {
                    if (AttributeContainsWildcard(yFromDisplayName))
                    {
                        // x is more specific
                        return -1;
                    }
                    else
                    {
                        // neither x nor y contains wildcard, 
                        return xFromDisplayName.CompareTo(yFromDisplayName);
                    }
                }
            }

            #endregion

            private bool AttributeContainsWildcard(string attribute)
            {
                return !string.IsNullOrEmpty(attribute) && attribute.Equals(UserIdentityMappingConfigSymbols.ANY, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
