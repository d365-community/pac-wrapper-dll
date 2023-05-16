# pac-wrapper-dll

## Introduction

The dll takes various params, see  ``Execute(string pac, params string[] args)``, Therefore, the wrapper is generic itself
```ps
PS C:\repos\ps-automation>Add-Type -Path '.\pac-wrapper.dll'
PS C:\repos\ps-automation>$result = [D365.Community.Pac.Wrapper.PacWrapper]::Execute($((get-command pac.launcher.exe).Path),"auth","list")
PS C:\repos\ps-automation>$result | convertfrom-json | convertto-json -depth 100
```

## Debug Pac Issues (wrapper)

```ps
PS C:\repos\ps-automation>#be careful, this may trace passwords, when calling e.g. pac auth create ...
PS C:\repos\ps-automation>$env:D365_PAC_TRACE = 'true'
```

```ps
PS C:\repos\ps-automation>#traces the pac response (stdout). 
PS C:\repos\ps-automation>$env:D365_PAC_DEBUG = 'true'
```
