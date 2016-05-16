// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyCompany("Microsoft")]
//
// IMPORTANT: Update the AssemblyProduct name for a major release.
//
// AssemblyProduct appears in the title bar of the shell among other places.
// The convention we use for major releases is date based major versions corresponding 
// to the release (e.g., (July 2010), (Sept 2010), etc.). 
//
[assembly: AssemblyProduct("TFS Integration Tools")]
[assembly: AssemblyCopyright("Copyright © Microsoft 2007-2010")]

[assembly: AssemblyVersion("2.0.31109.00")]
[assembly: AssemblyFileVersion("2.0.31109.00")]

#if EXCLUDE_ASSEMBLY_VERSION_INFO
#else
static class AssemblyVersionInfo
{
    public const string VersionString = "2.0.31109.00";
};
#endif

internal static class InternalsVisibleToKey
 {
#if !PARTIALLY_SIGNED_BUILD
    public const string PublicKey = "";
#else
    public const string PublicKey = ", PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9";
#endif
 }
