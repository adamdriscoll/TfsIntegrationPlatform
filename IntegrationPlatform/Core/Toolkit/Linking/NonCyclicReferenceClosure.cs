// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Linking
{
    public class NonCyclicReferenceClosure
    {
        private List<ILink> LinksInCollection { get; set; }
        private LinkComparer CurrLinkComparer { get; set; }
        private List<ILink> m_invalidLinks;
        private List<string> m_sourceArtifactUris;
        private List<string> m_targetArtifactUris;

        internal LinkType LinkType { get; set; }

        public ReadOnlyCollection<ILink> Links
        {
            get
            {
                return LinksInCollection.AsReadOnly();
            }
        }

        public ReadOnlyCollection<string> SourceArtifactUris 
        {
            get
            {
                return m_sourceArtifactUris.AsReadOnly();
            }
        }
        public ReadOnlyCollection<string> TargetArtifactUris
        {
            get
            {
                return m_targetArtifactUris.AsReadOnly();
            }
        }

        public ReadOnlyCollection<ILink> InvalidLinks
        {
            get
            {
                return m_invalidLinks.AsReadOnly();
            }
        }

        public NonCyclicReferenceClosure(LinkType linkType)
        {
            m_sourceArtifactUris = new List<string>();
            m_targetArtifactUris = new List<string>();
            LinkType = linkType;
            LinksInCollection = new List<ILink>();
            CurrLinkComparer = new LinkComparer();
            m_invalidLinks = new List<ILink>();
        }

        public void AddValidLink(ILink link)
        {
            if (null == link)
            {
                throw new ArgumentNullException("link");
            }

            if (!IsTypeOfTheCollection(link))
            {
                return;
            }

            if (LinkIntroducesCyclicReference(link))
            {
                return;
            }

            AddLink(link);
        }

        private void AddLink(ILink link)
        {
            if (!IsLinkInCollection(link))
            {
                LinksInCollection.Add(link);
                LinksInCollection.Sort(CurrLinkComparer);

                if (!SourceArtifactUris.Contains(link.SourceArtifact.Uri))
                {
                    m_sourceArtifactUris.Add(link.SourceArtifact.Uri);
                    m_sourceArtifactUris.Sort();
                }

                if (!TargetArtifactUris.Contains(link.TargetArtifact.Uri))
                {
                    m_targetArtifactUris.Add(link.TargetArtifact.Uri);
                    m_targetArtifactUris.Sort();
                }
            }
        }

        internal void AddLinkForAnalysis(ILink link)
        {
            if (null == link)
            {
                throw new ArgumentNullException("link");
            }

            if (!IsTypeOfTheCollection(link))
            {
                return;
            }

            AddLink(link);

            if (LinkIntroducesCyclicReference(link))
            {
                m_invalidLinks.Add(link);
            }
        }

        internal void DeleteLinkForAnaysis(ILink link)
        {
            if (null == link)
            {
                throw new ArgumentNullException("link");
            }

            if (!IsTypeOfTheCollection(link))
            {
                return;
            }

            DeleteLink(link);
        }

        private void DeleteLink(ILink link)
        {
            int pos = LinksInCollection.BinarySearch(link, CurrLinkComparer);
            if (pos >= 0)
            {
                LinksInCollection.RemoveAt(pos);

                if (SourceArtifactUris.Contains(link.SourceArtifact.Uri))
                {
                    m_sourceArtifactUris.Remove(link.SourceArtifact.Uri);
                }

                if (TargetArtifactUris.Contains(link.TargetArtifact.Uri))
                {
                    m_targetArtifactUris.Remove(link.TargetArtifact.Uri);
                }
            }
        }

        private bool LinkIntroducesCyclicReference(ILink link)
        {
            return TargetArtifactUris.Contains(link.SourceArtifact.Uri)
                   && SourceArtifactUris.Contains(link.TargetArtifact.Uri);
        }

        private bool IsLinkInCollection(ILink link)
        {
            int pos = LinksInCollection.BinarySearch(link, CurrLinkComparer);
            return pos >= 0;
        }

        private bool IsTypeOfTheCollection(ILink link)
        {
            return link.LinkType.ReferenceName.Equals(LinkType.ReferenceName);
        }
    }
}