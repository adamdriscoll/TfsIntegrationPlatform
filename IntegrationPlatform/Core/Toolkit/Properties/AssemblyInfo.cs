// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;
using System.Security.Permissions;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Toolkit")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("4834afe2-74a9-4d5c-b473-e3ee1986bc9f")]

[assembly: SecurityPermission(SecurityAction.RequestMinimum, Execution = true)]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1014:MarkAssembliesWithClsCompliant")]
[assembly: InternalsVisibleTo("Tests" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("Toolkit.AnalysisEngine" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("TfsIntegrationService" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("ServerDiff" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("VCServerDiffJob" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("WITServerDiffJob" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("MigrationConsole" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("SyncMonitorConsole" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("SyncMonitorJob" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("UnitTests" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("InternalUnitTests" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("Microsoft.TeamFoundation.Migration.Shell" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("MigrationTestLibrary" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("TfsVCTest" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("TfsWITTest" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("TfsFileSystemAdapterTest" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("BasicVCTest" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("BasicWITTest" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("Tfs2010WitTest" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("InternalTfsWitTest" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("PerfTest" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("ForceSyncWorkItems" + InternalsVisibleToKey.PublicKey)]








