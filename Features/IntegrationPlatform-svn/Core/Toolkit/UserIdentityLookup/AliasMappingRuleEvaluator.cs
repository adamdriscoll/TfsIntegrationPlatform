// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class AliasMappingRuleEvaluator : IUserIdMappingRuleEvaluator
    {
        Dictionary<MappingDirectionEnum, NotifyingCollection<AliasMapping>> m_perDirectionMappings
            = new Dictionary<MappingDirectionEnum, NotifyingCollection<AliasMapping>>();

        AliasMappingComparer m_mappingRuleComparer = new AliasMappingComparer();

        public AliasMappingRuleEvaluator(
            NotifyingCollection<AliasMappings> aliasMappings)
        {
            Initialize(aliasMappings);
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

        private void Initialize(NotifyingCollection<AliasMappings> aliasMappings)
        {
            foreach (AliasMappings mappingCollection in aliasMappings)
            {
                if (!m_perDirectionMappings.ContainsKey(mappingCollection.DirectionOfMapping))
                {
                    m_perDirectionMappings.Add(mappingCollection.DirectionOfMapping, new NotifyingCollection<AliasMapping>());
                }

                m_perDirectionMappings[mappingCollection.DirectionOfMapping].AddRange(mappingCollection.AliasMapping.ToArray());
            }
        }

        private bool MapUser(
            RichIdentity sourceUserIdentity,
            bool leftToRight,
            RichIdentity mappedUserIdentity)
        {
            m_mappingRuleComparer.MapFromLeftToRight = leftToRight;

            NotifyingCollection<AliasMapping> unidirectionalRules;
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

            AliasMapping appliedUnidirectionalRule = null;
            RichIdentity unidirectionalMappingOutput = null;
            foreach (AliasMapping rule in unidirectionalRules)
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
                mappedUserIdentity.Alias = unidirectionalMappingOutput.Alias;
                return true;
            }

            AliasMapping appliedBidirectionalRule = null;
            RichIdentity bidirectionalMappingOutput = null;
            if (m_perDirectionMappings.ContainsKey(MappingDirectionEnum.TwoWay))
            {
                foreach (AliasMapping rule in m_perDirectionMappings[MappingDirectionEnum.TwoWay])
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
                mappedUserIdentity.Alias = bidirectionalMappingOutput.Alias;
            }

            return mapped;
        }

        private bool TryApplyMappingRule(RichIdentity sourceUserIdentity, bool leftToRight, RichIdentity mappingOutput, AliasMapping rule)
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

        private bool ApplyRule(RichIdentity sourceUserIdentity, bool leftToRight, RichIdentity mappingOutput, AliasMapping rule)
        {
            string toAlias = (leftToRight ? rule.Right : rule.Left);
            IStringManipulationRule stringManipulationRule = StringManipulationRuleFactory.GetInstance(rule.MappingRule);

            if (stringManipulationRule != null)
            {
                mappingOutput.Alias = stringManipulationRule.Apply(sourceUserIdentity.Alias, toAlias);
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsRuleApplicable(RichIdentity sourceUserIdentity, AliasMapping rule, bool leftToRight)
        {
            string fromAlias = (leftToRight ? rule.Left : rule.Right);

            if (string.IsNullOrEmpty(fromAlias))
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

            return (fromAlias.Equals(UserIdentityMappingConfigSymbols.ANY, StringComparison.OrdinalIgnoreCase)
                   || fromAlias.Equals(sourceUserIdentity.Alias ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        }

        class AliasMappingComparer : IComparer<AliasMapping>
        {
            public bool MapFromLeftToRight { get; set; }

            #region IComparer<AliasMapping> Members

            public int Compare(AliasMapping x, AliasMapping y)
            {
                // return -1 if x is the more specific rule
                // return 1 if x is the less specific rule

                string xFromAlias = (MapFromLeftToRight ? x.Left : x.Right);
                string yFromAlias = (MapFromLeftToRight ? y.Left : y.Right);

                if (string.IsNullOrEmpty(xFromAlias) || string.IsNullOrEmpty(yFromAlias))
                {
                    // not comparable, assume to be equivalent
                    return 0;
                }

                if (AttributeContainsWildcard(xFromAlias))
                {
                    if (AttributeContainsWildcard(yFromAlias))
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
                    if (AttributeContainsWildcard(yFromAlias))
                    {
                        // x is more specific
                        return -1;
                    }
                    else
                    {
                        // neither x nor y contains wildcard, 
                        return xFromAlias.CompareTo(yFromAlias);
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
