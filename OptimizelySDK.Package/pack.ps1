Write-Host "Packing Optimizely SDK for NuGet"
Write-Host "-"
Write-Host "This script requires VS 2017 MSBuild & NuGet CLI"
Write-Host "-"

################################################################
# NuGet lib
################################################################
New-Item -Path ".\lib\net45" -ItemType "directory" -force
Copy-Item -Path "..\OptimizelySDK\bin\Release\Optimizely*.dll" -Destination ".\lib\net45" -Recurse -force
Copy-Item -Path "..\OptimizelySDK\bin\Release\Optimizely*.pdb" -Destination ".\lib\net45" -Recurse -force
Copy-Item -Path "..\OptimizelySDK\bin\Release\Optimizely*.xml" -Destination ".\lib\net45" -Recurse -force

New-Item -Path ".\lib\net40" -ItemType "directory" -force
Copy-Item -Path "..\OptimizelySDK.Net40\bin\Release\Optimizely*.dll" -Destination ".\lib\net40" -Recurse -force
Copy-Item -Path "..\OptimizelySDK.Net40\bin\Release\Optimizely*.pdb" -Destination ".\lib\net40" -Recurse -force
Copy-Item -Path "..\OptimizelySDK.Net40\bin\Release\Optimizely*.xml" -Destination ".\lib\net40" -Recurse -force

New-Item -Path ".\lib\net35" -ItemType "directory" -force
Copy-Item -Path "..\OptimizelySDK.Net35\bin\Release\Optimizely*.dll" -Destination ".\lib\net35" -Recurse -force
Copy-Item -Path "..\OptimizelySDK.Net35\bin\Release\Optimizely*.pdb" -Destination ".\lib\net35" -Recurse -force
Copy-Item -Path "..\OptimizelySDK.Net35\bin\Release\Optimizely*.xml" -Destination ".\lib\net35" -Recurse -force

New-Item -Path ".\lib\netstandard1.6" -ItemType "directory" -force
Copy-Item -Path "..\OptimizelySDK.NetStandard16\bin\Release\netstandard1.6\Optimizely*.dll" -Destination ".\lib\netstandard1.6" -Recurse -force
Copy-Item -Path "..\OptimizelySDK.NetStandard16\bin\Release\netstandard1.6\Optimizely*.pdb" -Destination ".\lib\netstandard1.6" -Recurse -force
Copy-Item -Path "..\OptimizelySDK.NetStandard16\bin\Release\netstandard1.6\Optimizely*.xml" -Destination ".\lib\netstandard1.6" -Recurse -force

New-Item -Path ".\lib\netstandard2.0" -ItemType "directory" -force
Copy-Item -Path "..\OptimizelySDK.NetStandard20\bin\Release\netstandard2.0\Optimizely*.dll" -Destination ".\lib\netstandard2.0" -Recurse -force
Copy-Item -Path "..\OptimizelySDK.NetStandard20\bin\Release\netstandard2.0\Optimizely*.pdb" -Destination ".\lib\netstandard2.0" -Recurse -force
Copy-Item -Path "..\OptimizelySDK.NetStandard20\bin\Release\netstandard2.0\Optimizely*.xml" -Destination ".\lib\netstandard2.0" -Recurse -force

################################################################
# Creating NuGet package
################################################################
Write-Host "-"
Write-Host "Creating NuGet package"
nuget pack OptimizelySDK.nuspec
