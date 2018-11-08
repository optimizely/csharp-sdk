Write-Host "Verify Strong Naming"
Write-Host "This script requires VS 2017"

################################################################
# Locate Tools (*.exe)
################################################################
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
# Locate *.nupkg
################################################################
Write-Host "Locate *.nupkg"
# Good enough for 2.2.1
$nupkg="./Optimizely.SDK.2.2.1.nupkg"

################################################################
# Unzipping *.nupkg
################################################################
Write-Host "Unzipping *.nupkg"
New-Item -Path "./VerifySn" -ItemType "directory" -force
Expand-Archive -Path $nupkg -DestinationPath "./VerifySn"

################################################################
# Verify Strong Names (sn.exe)
################################################################
& $sn -v "./VerifySn/lib/net35/OptimizelySDK.Net35.dll"
& $sn -v "./VerifySn/lib/net40/OptimizelySDK.Net40.dll"
& $sn -v "./VerifySn/lib/net45/OptimizelySDK.dll"
& $sn -v "./VerifySn/lib/netstandard1.6/OptimizelySDK.NetStandard16.dll"
