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
    class UserMappingRuleEvaluator : IUserIdMappingRuleEvaluator
    {
        Dictionary<MappingDirectionEnum, NotifyingCollection<UserMapping>> m_perDirectionUserMappings 
            = new Dictionary<MappingDirectionEnum, NotifyingCollection<UserMapping>>();

        UserMappingComparer m_mappingRuleComparer = new UserMappingComparer();

        public UserMappingRuleEvaluator(
            NotifyingCollection<UserMappings> userMappings)
        {
            Initialize(userMappings);
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

        private void Initialize(NotifyingCollection<UserMappings> userMappings)
        {
            foreach (UserMappings userMappingCollection in userMappings)
            {
                if (!m_perDirectionUserMappings.ContainsKey(userMappingCollection.DirectionOfMapping))
                {
                    m_perDirectionUserMappings.Add(userMappingCollection.DirectionOfMapping, new NotifyingCollection<UserMapping>());
                }

                m_perDirectionUserMappings[userMappingCollection.DirectionOfMapping].AddRange(userMappingCollection.UserMapping.ToArray());
            }
        }

        private bool MapUser(
            RichIdentity sourceUserIdentity, 
            bool leftToRight, 
            RichIdentity mappedUserIdentity)
        {
            m_mappingRuleComparer.MapFromLeftToRight = leftToRight;

            NotifyingCollection<UserMapping> unidirectionalRules;
            if (leftToRight)
            {
                if (!m_perDirectionUserMappings.ContainsKey(MappingDirectionEnum.LeftToRight))
                {
                    return false;
                }
                unidirectionalRules = m_perDirectionUserMappings[MappingDirectionEnum.LeftToRight];
            }
            else
            {
                if (!m_perDirectionUserMappings.ContainsKey(MappingDirectionEnum.RightToLeft))
                {
                    return false;
                }
                unidirectionalRules = m_perDirectionUserMappings[MappingDirectionEnum.RightToLeft];
            }

            bool mapped = false;
            UserMapping appliedUnidirectionalRule = null;
            RichIdentity unidirectionalMappingOutput = null;
            foreach (UserMapping rule in unidirectionalRules)
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

            #region teyang comment out this if clause if we do not want to give evaluation precedence to unidirectional mapping rules
            if (mapped)
            {
                mappedUserIdentity.Alias = unidirectionalMappingOutput.Alias;
                mappedUserIdentity.Domain = unidirectionalMappingOutput.Domain;
                return true;
            } 
            #endregion
            

            UserMapping appliedBidirectionalRule = null;
            RichIdentity bidirectionalMappingOutput = null;
            if (m_perDirectionUserMappings.ContainsKey(MappingDirectionEnum.TwoWay))
            {
                foreach (UserMapping rule in m_perDirectionUserMappings[MappingDirectionEnum.TwoWay])
                {
                    RichIdentity mappingOutput = new RichIdentity();
                    if (TryApplyMappingRule(sourceUserIdentity, leftToRight, mappingOutput, rule))
                    {
                        if (appliedBidirectionalRule == null
                        || m_mappingRuleComparer.Compare(rule, appliedUnidirectionalRule) < 0)
                        {
                            appliedUnidirectionalRule = rule;
                            bidirectionalMappingOutput = mappingOutput;
                        }
                        mapped = true;
                    }
                }
            }

            #region teyang comment out this if clause if we do not want to give evaluation precedence to unidirectional mapping rules
            if (mapped)
            {
                mappedUserIdentity.Alias = bidirectionalMappingOutput.Alias;
                mappedUserIdentity.Domain = bidirectionalMappingOutput.Domain;
            }
            #endregion


            #region teyang UNcomment this if clause out if we do not want to give evaluation precedence to unidirectional mapping rules
            //if (mapped)
            //{
            //    if (appliedBidirectionalRule != null && appliedUnidirectionalRule != null)
            //    {
            //        if (m_mappingRuleComparer.Compare(appliedBidirectionalRule, appliedUnidirectionalRule) < 0)
            //        {
            //            mappedUserIdentity = bidirectionalMappingOutput;
            //        }
            //        else
            //        {
            //            mappedUserIdentity = unidirectionalMappingOutput;
            //        }
            //    }
            //    else if (appliedBidirectionalRule != null)
            //    {
            //        mappedUserIdentity = bidirectionalMappingOutput;
            //    }
            //    else
            //    {
            //        Debug.Assert(null != unidirectionalMappingOutput, "null == unidirectionalMappingOutput");
            //        mappedUserIdentity = unidirectionalMappingOutput;
            //    }
            //}
            
            #endregion

            return mapped;
        }

        private bool TryApplyMappingRule(
            RichIdentity sourceUserIdentity, 
            bool leftToRight, 
            RichIdentity mappingOutput,
            UserMapping rule)
        {
            if (IsRuleApplicable(sourceUserIdentity, rule, leftToRight))
            {
                ApplyRule(sourceUserIdentity, leftToRight, mappingOutput, rule);
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsRuleApplicable(RichIdentity sourceUserIdentity, UserMapping rule, bool leftToRight)
        {
            string fromAlias = (leftToRight ? rule.LeftUser.Alias : rule.RightUser.Alias);
            string fromDomain = (leftToRight ? rule.LeftUser.Domain : rule.RightUser.Domain);
            string toAlias = (leftToRight ? rule.RightUser.Alias : rule.LeftUser.Alias);

            if (string.IsNullOrEmpty(fromAlias) || string.IsNullOrEmpty(toAlias))
            {
                Debug.Assert(false, "Alias in a user mapping rule should always be an Non-empty string");
                return false;
            }

            bool aliasRuleIsApplicable = (fromAlias.Equals(UserIdentityMappingConfigSymbols.ANY, StringComparison.OrdinalIgnoreCase) 
                                         || fromAlias.Equals(sourceUserIdentity.Alias ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            if (!aliasRuleIsApplicable)
            {
                return false;
            }

            bool domainRuleIsApplicable = (string.IsNullOrEmpty(fromDomain) 
                                          || fromDomain.Equals(UserIdentityMappingConfigSymbols.ANY, StringComparison.OrdinalIgnoreCase) 
                                          || fromDomain.Equals(sourceUserIdentity.Domain ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            return (aliasRuleIsApplicable && domainRuleIsApplicable);
        }

        private void ApplyRule(RichIdentity sourceUserIdentity, bool leftToRight, RichIdentity mappedUserIdentity, UserMapping rule)
        {
            string toAlias = (leftToRight ? rule.RightUser.Alias : rule.LeftUser.Alias);
            string toDomain = (leftToRight ? rule.RightUser.Domain : rule.LeftUser.Domain);

            if (toAlias.Equals(UserIdentityMappingConfigSymbols.ANY, StringComparison.OrdinalIgnoreCase))
            {
                mappedUserIdentity.Alias = sourceUserIdentity.Alias;
            }
            else
            {
                mappedUserIdentity.Alias = toAlias;
            }

            if (string.IsNullOrEmpty(toDomain))
            {
                mappedUserIdentity.Domain = string.Empty;
            }
            else if (toDomain.Equals(UserIdentityMappingConfigSymbols.ANY, StringComparison.OrdinalIgnoreCase))
            {
                mappedUserIdentity.Domain = sourceUserIdentity.Domain;
            }
            else
            {
                mappedUserIdentity.Domain = toDomain;
            }
        }

        class UserMappingComparer : IComparer<UserMapping>
        {
            public bool MapFromLeftToRight { get; set; }

            #region IComparer<UserMapping> Members

            public int Compare(UserMapping x, UserMapping y)
            {
                // return -1 if x is the more specific rule

                // return 1 if x is the less specific rule

                // return 0 if x and y are equivalent, i.e. both having the same wildcard char 
                // on the same attr, Alias or Domain. for instance:
                // x: Alias = "*"; Domain = "*"
                // y: Alias = "*"; Domain = "*"

                // Alias is considered more specific than domain, -1 is returned in the following case:
                // x: Alias = "johnsm"; Domain = "*"
                // y: Alias = "*"; Domain = "redmond"

                if (!ContainsWildCardChar(x) && !ContainsWildCardChar(y))
                {
                    // neither x nor y has wildcard
                    return x.ToString().CompareTo(y.ToString());
                }
                               
                if (ContainsWildCardChar(x))
                {
                    if (!ContainsWildCardChar(y))
                    {
                        // x is less specific: x contains wildcard but y does not
                        return 1;
                    }
                    else
                    {
                        // both x and y contain wildcard 
                        return CompareRulesWithWildcard(x, y, MapFromLeftToRight);
                    }
                }
                else // !ContainsWildCardChar(x)
                {
                    if (ContainsWildCardChar(y))
                    {
                        // x is more specific: x doesn't contain wildcard but y does
                        return -1;
                    }
                    else
                    {
                        // neither x nor y contains wildcard
                        return 0;
                    }
                }
           
            }

            #endregion
            
            private int CompareRulesWithWildcard(UserMapping x, UserMapping y, bool mapFromLeftToRight)
            {
                if (mapFromLeftToRight)
                {
                    bool xLeftAliasHasWildcard = AttributeContainsWildcard(x.LeftUser.Alias);
                    bool xLeftDomainHasWildcard = AttributeContainsWildcard(x.LeftUser.Domain);
                    bool yLeftAliasHasWildcard = AttributeContainsWildcard(y.LeftUser.Alias);
                    bool yLeftDomainHasWildcard = AttributeContainsWildcard(y.LeftUser.Domain);

                    return CompareRulesWithWildcard(xLeftAliasHasWildcard, xLeftDomainHasWildcard, yLeftAliasHasWildcard, yLeftDomainHasWildcard);
                }
                else
                {
                    bool xRightAliasHasWildcard = AttributeContainsWildcard(x.RightUser.Alias);
                    bool xRightDomainHasWildcard = AttributeContainsWildcard(x.RightUser.Domain);
                    bool yRightAliasHasWildcard = AttributeContainsWildcard(y.RightUser.Alias);
                    bool yRightDomainHasWildcard = AttributeContainsWildcard(y.RightUser.Domain);

                    return CompareRulesWithWildcard(xRightAliasHasWildcard, xRightDomainHasWildcard, yRightAliasHasWildcard, yRightDomainHasWildcard);
                }
            }

            private static int CompareRulesWithWildcard(
                bool xFromAliasHasWildcard, 
                bool xFromDomainHasWildcard, 
                bool yFromAliasHasWildcard, 
                bool yFromDomainHasWildcard)
            {
                if (xFromAliasHasWildcard)
                {
                    if (!yFromAliasHasWildcard)
                    {
                        // x is less specific than y in 'Alias' attribute
                        return 1;
                    }
                    else
                    {
                        if (xFromDomainHasWildcard)
                        {
                            if (yFromDomainHasWildcard)
                            {
                                return 0;
                            }
                            else
                            {
                                // x is less specific than y in 'Domain attribute
                                return 1;
                            }
                        }
                        else
                        {
                            if (yFromDomainHasWildcard)
                            {
                                // x is more specific than y in 'Domain attribute
                                return -1;
                            }
                            else
                            {
                                Debug.Assert(false, "neither x nor y contains wildcard");
                                return 0;
                            }
                        }
                    }
                }
                else // !xFromAliasHasWildcard
                {
                    if (!yFromAliasHasWildcard)
                    {
                        Debug.Assert(false, "neither x nor y contains wildcard");
                        return 0;
                    }
                    else
                    {
                        // x is more specific than y in 'Alias' attribute
                        return -1;
                    }
                }
            }

            private bool ContainsWildCardChar(UserMapping mappingRule)
            {
                bool leftAliasHasWildcard = AttributeContainsWildcard(mappingRule.LeftUser.Alias);
                bool leftDomainHasWildcard = AttributeContainsWildcard(mappingRule.LeftUser.Domain);
                bool rightAliasHasWildcard = AttributeContainsWildcard(mappingRule.RightUser.Alias);
                bool rightDomainHasWildcard = AttributeContainsWildcard(mappingRule.RightUser.Domain);

                return (leftAliasHasWildcard || leftDomainHasWildcard || rightAliasHasWildcard || rightDomainHasWildcard);
            }

            private bool AttributeContainsWildcard(string attribute)
            {
                return !string.IsNullOrEmpty(attribute) && attribute.Equals(UserIdentityMappingConfigSymbols.ANY, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
