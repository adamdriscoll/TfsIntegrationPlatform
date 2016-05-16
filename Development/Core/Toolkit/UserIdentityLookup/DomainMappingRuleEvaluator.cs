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
    class DomainMappingRuleEvaluator : IUserIdMappingRuleEvaluator
    {
        Dictionary<MappingDirectionEnum, NotifyingCollection<DomainMapping>> m_perDirectionMappings
            = new Dictionary<MappingDirectionEnum, NotifyingCollection<DomainMapping>>();

        DomainMappingComparer m_mappingRuleComparer = new DomainMappingComparer();

        public DomainMappingRuleEvaluator(
            NotifyingCollection<DomainMappings> domainMappings)
        {
            Initialize(domainMappings);
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

        private void Initialize(NotifyingCollection<DomainMappings> domainMappings)
        {
            foreach (DomainMappings mappingCollection in domainMappings)
            {
                if (!m_perDirectionMappings.ContainsKey(mappingCollection.DirectionOfMapping))
                {
                    m_perDirectionMappings.Add(mappingCollection.DirectionOfMapping, new NotifyingCollection<DomainMapping>());
                }

                m_perDirectionMappings[mappingCollection.DirectionOfMapping].AddRange(mappingCollection.DomainMapping.ToArray());
            }
        }

        private bool MapUser(
            RichIdentity sourceUserIdentity,
            bool leftToRight,
            RichIdentity mappedUserIdentity)
        {
            m_mappingRuleComparer.MapFromLeftToRight = leftToRight;

            NotifyingCollection<DomainMapping> unidirectionalRules;
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

            DomainMapping appliedUnidirectionalRule = null;
            RichIdentity unidirectionalMappingOutput = null;
            foreach (DomainMapping rule in unidirectionalRules)
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
                mappedUserIdentity.Domain = unidirectionalMappingOutput.Domain;
                return true;
            }

            DomainMapping appliedBidirectionalRule = null;
            RichIdentity bidirectionalMappingOutput = null;
            if (m_perDirectionMappings.ContainsKey(MappingDirectionEnum.TwoWay))
            {
                foreach (DomainMapping rule in m_perDirectionMappings[MappingDirectionEnum.TwoWay])
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
                mappedUserIdentity.Domain = bidirectionalMappingOutput.Domain;
            }

            return mapped;
        }

        private bool TryApplyMappingRule(RichIdentity sourceUserIdentity, bool leftToRight, RichIdentity mappingOutput, DomainMapping rule)
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

        private bool ApplyRule(RichIdentity sourceUserIdentity, bool leftToRight, RichIdentity mappingOutput, DomainMapping rule)
        {
            string toDomainName = (leftToRight ? rule.Right : rule.Left);
            IStringManipulationRule stringManipulationRule = StringManipulationRuleFactory.GetInstance(rule.MappingRule);

            if (stringManipulationRule != null)
            {
                mappingOutput.Domain = stringManipulationRule.Apply(sourceUserIdentity.Domain, toDomainName);
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsRuleApplicable(RichIdentity sourceUserIdentity, DomainMapping rule, bool leftToRight)
        {
            string fromDomainName = (leftToRight ? rule.Left : rule.Right);

            if (string.IsNullOrEmpty(fromDomainName))
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

            return (fromDomainName.Equals(UserIdentityMappingConfigSymbols.ANY, StringComparison.OrdinalIgnoreCase)
                   || fromDomainName.Equals(sourceUserIdentity.Domain ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        }

        class DomainMappingComparer : IComparer<DomainMapping>
        {
            public bool MapFromLeftToRight { get; set; }

            #region IComparer<DomainMapping> Members

            public int Compare(DomainMapping x, DomainMapping y)
            {
                // return -1 if x is the more specific rule
                // return 1 if x is the less specific rule

                string xFromDomainName = (MapFromLeftToRight ? x.Left : x.Right);
                string yFromDomainName = (MapFromLeftToRight ? y.Left : y.Right);

                if (string.IsNullOrEmpty(xFromDomainName) || string.IsNullOrEmpty(yFromDomainName))
                {
                    // not comparable, assume to be equivalent
                    return 0;
                }

                if (AttributeContainsWildcard(xFromDomainName))
                {
                    if (AttributeContainsWildcard(yFromDomainName))
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
                    if (AttributeContainsWildcard(yFromDomainName))
                    {
                        // x is more specific
                        return -1;
                    }
                    else
                    {
                        // neither x nor y contains wildcard, 
                        return xFromDomainName.CompareTo(yFromDomainName);
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
