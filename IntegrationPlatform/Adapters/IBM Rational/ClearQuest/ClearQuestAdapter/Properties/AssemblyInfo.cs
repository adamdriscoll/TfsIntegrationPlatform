// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("ClearQuestAdapter")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("a1607515-98f4-4080-bb4f-be7d83e4ed93")]

// NOTE: Assembly version info comes from common linked version.cs

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TestCQAdapters" + InternalsVisibleToKey.PublicKey)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ClearQuestV7TCAdapter" + InternalsVisibleToKey.PublicKey)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InternalUnitTests" + InternalsVisibleToKey.PublicKey)]