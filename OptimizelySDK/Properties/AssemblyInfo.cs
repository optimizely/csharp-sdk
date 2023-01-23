using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("OptimizelySDK")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("OptimizelySDK")]
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
[assembly: InternalsVisibleTo("OptimizelySDK.Tests")]
[assembly: InternalsVisibleTo("OptimizelySDK.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100355b847429963d253d4ab3aa0fdcf8ca7010815857a9de53c262eef801de76e7be4e294e487daee54e535d530ace0d4918b75cfc470142630058ba34a327eefc896ec232301c4e1d16fcf35e292c3a0bdec6b43a0e2d85f679efd4ee77465a23cf866bebafd814e5d555ffbac66f228a75ef60aefffe51c6091103d1455febf5")]
#pragma warning restore 1700

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("4dde7faa-110d-441c-ab3b-3f31b593e8bf")]

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
[assembly: AssemblyVersion("3.11.1.0")]
[assembly: AssemblyFileVersion("3.11.1.0")]
[assembly: AssemblyInformationalVersion("3.11.1")] // Used by Nuget.
