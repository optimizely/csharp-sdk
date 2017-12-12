Write-Host "Packing Optimizely SDK for NuGet"
Write-Host "-"
Write-Host "This script requires VS 2017 MSBuild & NuGet CLI"
Write-Host "-"

################################################################
# Locate Tools (*.exe)
################################################################
if ($PSVersionTable["Platform"] -eq "Unix") {
    # Including macOS
    $msbuild="/Library/Frameworks/Mono.framework/Versions/Current/Commands/msbuild"
} elseif (Test-Path "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise") {
    $msbuild="C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\msbuild.exe"
} elseif (Test-Path "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community") {
    $msbuild="C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\msbuild.exe"
} else {
    Write-Host "Unable to locate msbuild.exe"
    Exit 1
}

if ($PSVersionTable["Platform"] -eq "Unix") {
    # Including macOS
    $sn="/Library/Frameworks/Mono.framework/Versions/Current/Commands/sn"
} elseif (Test-Path "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.2 Tools\x64") {
    $sn="C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.2 Tools\x64\sn.exe"
} else {
    Write-Host "Unable to locate sn.exe"
    Exit 1
}

################################################################
# Compiling (msbuild.exe)
################################################################
& $msbuild ..\OptimizelySDK.sln /p:Platform="Any CPU" /p:Configuration=Release /p:GenerateDocumentation=true  /t:Clean,Build /nr:false /clp:Summary
Write-Host "-"
Write-Host "Build complete. Copying files..."

################################################################
# Strong Naming (sn.exe)
################################################################
# One can use 'find . -name "Optimizely*.dll" -print | grep "bin/Release" | grep -v ".Tests"'
# to find the Optimizely*.dll that we need this PowerShell script to strongname (sn.exe).
& $sn  -R "../OptimizelySDK/bin/Release/OptimizelySDK.dll" "../keypair.snk"
& $sn  -R "../OptimizelySDK.Net35/bin/Release/OptimizelySDK.Net35.dll" "../keypair.snk"
& $sn  -R "../OptimizelySDK.Net40/bin/Release/OptimizelySDK.Net40.dll" "../keypair.snk"
& $sn  -R "../OptimizelySDK.NetStandard16/bin/Release/netstandard1.6/OptimizelySDK.NetStandard16.dll" "../keypair.snk"

################################################################
# NuGet lib
################################################################
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

################################################################
# Digital Signing (signtool.exe)
################################################################
if ($PSVersionTable["Platform"] -eq "Unix") {
    # Including macOS
} elseif (Test-Path "C:\Program Files (x86)\Windows Kits\10\bin\x64") {
    $signtool="C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe"
    Write-Host "-"
    Write-Host "Digitally Signing (signtool.exe)"
    # One can use 'find "" -name "*.dll" -print  '
    # to find the *.dll that we need this PowerShell script to digitally sign (signtool.exe).
    & $signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /n "Optimizely, Inc." ".\lib\net35\OptimizelySDK.Net35.dll"
    & $signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /n "Optimizely, Inc." ".\lib\net40\OptimizelySDK.Net40.dll"
    & $signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /n "Optimizely, Inc." ".\lib\net45\OptimizelySDK.dll"
    & $signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /n "Optimizely, Inc." ".\lib\netstandard1.6\OptimizelySDK.NetStandard16.dll"
}

################################################################
# Creating NuGet package
################################################################
Write-Host "-"
Write-Host "Creating NuGet package"
nuget pack OptimizelySDK.nuspec
