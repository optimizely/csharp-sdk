$msBuildLocation="C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\msbuild.exe"

Write-Host "Packing Optimizely SDK for NuGet"
Write-Host "-"
Write-Host "This script requires VS 2017 Community Edition & NuGet CLI"
Write-Host "-"
Write-Host "-"

& $msBuildLocation ..\OptimizelySDK.sln /p:Platform="Any CPU" /p:Configuration=Release /p:GenerateDocumentation=true  /t:Clean,Build /nr:false /clp:Summary

Write-Host "-"
Write-Host "-"
Write-Host "Build complete. Copying files..."

Copy-Item  -Path "..\OptimizelySDK\bin\Release\OptimizelySDK.dll" -Destination ".\lib\net45"  -Recurse -force
Copy-Item  -Path "..\OptimizelySDK.Net35\bin\Release\OptimizelySDK.Net35.dll" -Destination ".\lib\net35"  -Recurse -force
Copy-Item  -Path "..\OptimizelySDK.Net35\bin\Release\OptimizelySDK.Net35.dll" -Destination ".\lib\net40\"  -Recurse -force
Copy-Item  -Path "..\OptimizelySDK.NetStandard16\bin\Release\netstandard1.6\OptimizelySDK.NetStandard16.dll" -Destination ".\lib\netstandard1.6"  -Recurse -force


Write-Host "-"
Write-Host "-"
Write-Host "Creating NuGet package"

nuget pack OptimizelySDK.nuspec
