﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("OptimizelySDK.Net35")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("OptimizelySDK.Net35")]
[assembly: AssemblyCopyright("Copyright © 2017-2020")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// Make types and members with internal scope visible to friend
// OptimizelySDK.Tests unit tests.
#pragma warning disable 1700
[assembly: InternalsVisibleTo("OptimizelySDK.Tests, PublicKey=ThePublicKey")]
#pragma warning restore 1700

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("c8ff7012-37b7-4d64-ab45-0c62195302ec")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
[assembly: AssemblyVersion("3.11.4.0")]
[assembly: AssemblyFileVersion("3.11.4.0")]
[assembly: AssemblyInformationalVersion("3.11.4")] // Used by Nuget.
