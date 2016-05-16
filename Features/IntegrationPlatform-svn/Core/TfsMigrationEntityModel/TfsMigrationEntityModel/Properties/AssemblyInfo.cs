// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("TfsMigrationEntityModel")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d568a97f-9aae-48c1-8833-a28e722a67c9")]

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("UnitTests" + InternalsVisibleToKey.PublicKey)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ServerDiff" + InternalsVisibleToKey.PublicKey)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("VCServerDiffJob" + InternalsVisibleToKey.PublicKey)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("WITServerDiffJob" + InternalsVisibleToKey.PublicKey)]

// NOTE: Assembly version info comes from common linked version.cs