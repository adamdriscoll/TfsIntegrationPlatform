// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("MigrationTestLibrary")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Microsoft")]
[assembly: AssemblyProduct("MigrationTestLibrary")]
[assembly: AssemblyCopyright("Copyright © Microsoft 2009")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("68a94a90-6a40-4b88-9e87-ebe325e1d5f8")]

[assembly: InternalsVisibleTo("TfsVCTest" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("TfsWITTest" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("TfsFileSystemAdapterTest" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("BasicVCTest" + InternalsVisibleToKey.PublicKey)]
[assembly: InternalsVisibleTo("BasicWITTest" + InternalsVisibleToKey.PublicKey)]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
