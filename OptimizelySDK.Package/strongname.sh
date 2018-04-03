#!/bin/bash
################################################################
#    strongname.sh (Strong Naming)
################################################################
# One can use 'find . -name "Optimizely*.dll" -print | grep "bin/Release" | grep -v ".Tests"'
# to find the Optimizely*.dll that we need this PowerShell script to strongname (sn.exe).

set -e

cleanup() {
  rm -f "${tempfiles[@]}"
}
trap cleanup 0

error() {
  local lineno="$1"
  local message="$2"
  local code="${3:-1}"
  if [[ -n "${message}" ]] ; then
    echo "Error on line ${lineno}: ${message}; status ${code}"
  else
    echo "Error on line ${lineno}; status ${code}"
  fi
  exit "${code}"
}
trap 'error ${LINENO}' ERR

main() {
  if [ "$(uname)" != "Darwin" ]; then
    echo "${0} MUST be run on a Mac."
    exit 1
  fi
  sn -R "../OptimizelySDK/bin/Release/OptimizelySDK.dll" "../keypair.snk"
  sn -R "../OptimizelySDK.Net35/bin/Release/OptimizelySDK.Net35.dll" "../keypair.snk"
  sn -R "../OptimizelySDK.Net40/bin/Release/OptimizelySDK.Net40.dll" "../keypair.snk"
  sn -R "../OptimizelySDK.NetStandard16/bin/Release/netstandard1.6/OptimizelySDK.NetStandard16.dll" "../keypair.snk"
  sn -v "../OptimizelySDK/bin/Release/OptimizelySDK.dll"
  sn -v "../OptimizelySDK.Net35/bin/Release/OptimizelySDK.Net35.dll"
  sn -v "../OptimizelySDK.Net40/bin/Release/OptimizelySDK.Net40.dll"
  sn -v "../OptimizelySDK.NetStandard16/bin/Release/netstandard1.6/OptimizelySDK.NetStandard16.dll"
  cleanup
}

main
