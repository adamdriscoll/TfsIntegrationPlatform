// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Linking
{
    [Serializable]
    public class ExtendedLinkProperties
    {
        [Flags]
        public enum Topology
        {
            Network = 0,
            DirectedNetwork = ToplogyRuleAndDirectionality.Directed,
            Dependency = ToplogyRuleAndDirectionality.Directed | ToplogyRuleAndDirectionality.NonCircular,
            Tree = ToplogyRuleAndDirectionality.Directed | ToplogyRuleAndDirectionality.NonCircular | ToplogyRuleAndDirectionality.OnlyOneParent,
        };

        public ExtendedLinkProperties()
        {}

        public ExtendedLinkProperties(Topology topology)
        {
            LinkTopology = topology;
            switch (topology)
            {
                case Topology.Network:
                    Directed = NonCircular = HasOnlyOneParent = false;
                    break;
                case Topology.DirectedNetwork:
                    Directed = true;
                    NonCircular = HasOnlyOneParent = false;
                    break;
                case Topology.Dependency:
                    Directed = NonCircular = true;
                    HasOnlyOneParent = false;
                    break;
                case Topology.Tree:
                    Directed = NonCircular = HasOnlyOneParent = true;
                    break;
                default:
                    throw new ArgumentException(MigrationToolkitResources.ErrorLinkTopologyIsUnknown, "topology");
            }
        }

        [Flags]
        public enum ToplogyRuleAndDirectionality
        {
            Directed = 1,
            NonCircular = 1 << 1,
            OnlyOneParent = 1 << 2,
        }

        public Topology LinkTopology
        {
            get; 
            set;
        }

        public bool Directed
        {
            get; 
            set;
        }

        public bool NonCircular
        {
            get; 
            set;
        }

        public bool HasOnlyOneParent
        {
            get; 
            set;
        }
    }
}