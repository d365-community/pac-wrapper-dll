# pac-wrapper-dll

## Introduction 
```
Add-Type -Path '.\pac-wrapper.dll'
$result = [D365.Community.Pac.Wrapper.PacWrapper]::Execute($((get-command pac.launcher.exe).Path),"auth","list")
$result | convertfrom-json | convertto-json -depth 100
```