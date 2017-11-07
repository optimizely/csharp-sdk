NOTE: Regarding OptimizelySDK.Package/netstandard1.6

* Currently, we want Optimizely's nupkg to pick up
    ~/.nuget/packages/murmurhash-signed/1.0.2/lib/netstandard1.4/MurmurHash.dll
    ~/.nuget/packages/newtonsoft.json/9.0.1/lib/netstandard1.0/Newtonsoft.Json.dll
                                                               Newtonsoft.Json.xml
    ~/.nuget/packages/njsonschema/8.33.6323.36213/lib/netstandard1.0/NJsonSchema.dll
                                                                    /NJsonSchema.pdb
                                                                    /NJsonSchema.xml
* These are brittle locations to be grabbing binaries from.
* Therefore, we've precopied these binaries to directory
OptimizelySDK.Package/netstandard1.6 which is used as the source
of these binaries by OptimizelySDK.Package/pack.ps1 .
* These third party libaries are covered by the licenses included
in the Licenses subdirectory.
