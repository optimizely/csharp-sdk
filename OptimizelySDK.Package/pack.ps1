Write-Host "Packing Optimizely SDK for NuGet"
Write-Host "-"
Write-Host "This script requires VS 2017 MSBuild & NuGet CLI"
Write-Host "-"

if ($PSVersionTable["Platform"] -eq "Unix") {
    # Including macOS
    $msBuildLocation="/Library/Frameworks/Mono.framework/Versions/Current/Commands/msbuild"
} else {
    $msBuildLocation="C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\msbuild.exe"
}

& $msBuildLocation ..\OptimizelySDK.sln /p:Platform="Any CPU" /p:Configuration=Release /p:GenerateDocumentation=true  /t:Clean,Build /nr:false /clp:Summary

Write-Host "-"
Write-Host "Build complete. Copying files..."

New-Item -Path ".\content" -ItemType "directory" -force
Copy-Item -Path "..\licenses" -Destination ".\content" -Recurse -force
New-Item -Path ".\content\licenses\Optimizely" -ItemType "directory" -force
Copy-Item -Path "..\LICENSE" -Destination ".\content\licenses\Optimizely" -force

New-Item -Path ".\lib\net45" -ItemType "directory" -force
Copy-Item -Path "..\OptimizelySDK\bin\Release\*.dll" -Destination ".\lib\net45" -Recurse -force
Copy-Item -Path "..\OptimizelySDK\bin\Release\*.pdb" -Destination ".\lib\net45" -Recurse -force
Copy-Item -Path "..\OptimizelySDK\bin\Release\*.xml" -Destination ".\lib\net45" -Recurse -force

New-Item -Path ".\lib\net40" -ItemType "directory" -force
Copy-Item -Path "..\OptimizelySDK.Net40\bin\Release\*.dll" -Destination ".\lib\net40" -Recurse -force
Copy-Item -Path "..\OptimizelySDK.Net40\bin\Release\*.pdb" -Destination ".\lib\net40" -Recurse -force
Copy-Item -Path "..\OptimizelySDK.Net40\bin\Release\*.xml" -Destination ".\lib\net40" -Recurse -force

New-Item -Path ".\lib\net35" -ItemType "directory" -force
Copy-Item -Path "..\OptimizelySDK.Net35\bin\Release\*.dll" -Destination ".\lib\net35" -Recurse -force
Copy-Item -Path "..\OptimizelySDK.Net35\bin\Release\*.pdb" -Destination ".\lib\net35" -Recurse -force
Copy-Item -Path "..\OptimizelySDK.Net35\bin\Release\*.xml" -Destination ".\lib\net35" -Recurse -force

New-Item -Path ".\lib\netstandard1.6" -ItemType "directory" -force
Copy-Item -Path "..\OptimizelySDK.NetStandard16\bin\Release\netstandard1.6\*.dll" -Destination ".\lib\netstandard1.6" -Recurse -force
Copy-Item -Path "..\OptimizelySDK.NetStandard16\bin\Release\netstandard1.6\*.pdb" -Destination ".\lib\netstandard1.6" -Recurse -force
Copy-Item -Path "..\OptimizelySDK.NetStandard16\bin\Release\netstandard1.6\*.xml" -Destination ".\lib\netstandard1.6" -Recurse -force

Write-Host "-"
Write-Host "Creating NuGet package"

nuget pack OptimizelySDK.nuspec
