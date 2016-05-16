// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Shell.ViewModel
{
    public class OneWaySessionViewModel : ModelObject
    {
        public RTMigrationSource Source { get; private set; }

        public RTMigrationSource Target { get; private set; }

        public RTSessionConfig Session { get; private set; }

        public OneWaySessionViewModel(RTSessionConfig session, RTMigrationSource source, RTMigrationSource target)
        {
            Session = session;
            Source = source;
            Target = target;
        }

        public string FriendlyName
        {
            get
            {
                return string.Format("{0} -> {1}", Source.FriendlyName, Target.FriendlyName);
            }
        }
    }
}
