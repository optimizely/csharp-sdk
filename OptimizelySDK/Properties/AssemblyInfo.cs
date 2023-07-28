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
#if DEBUG
[assembly: InternalsVisibleTo("OptimizelySDK.Tests")]
#else
[assembly: InternalsVisibleTo("OptimizelySDK.Tests, PublicKey=00240000048000009400000006020000002400005253413100040000010001006b0705c5f697a2522639be5d5bc02835aaef2e2cd4adf47c3bbf5ed97187298c17448701597b5a610d29eed362f36f056062bbccd424fc830dd5966a9378302c61e3ddd77effcd9dcfaf739f3ca88149e961f55f23d5ce1948703da33e261f6cc0c681a19ce62ccbfdeca8bd286f93395e4f67e4a2ea7782af581062edab8083")]
#endif
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
[assembly: AssemblyVersion("3.11.4.0")]
[assembly: AssemblyFileVersion("3.11.4.0")]
[assembly: AssemblyInformationalVersion("3.11.4")] // Used by Nuget.
