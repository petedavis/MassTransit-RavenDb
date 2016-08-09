@echo off
cls

SETLOCAL
SET NUGET_VERSION=latest
SET CACHED_NUGET="%LocalAppData%\NuGet\nuget.%NUGET_VERSION%.exe"

IF EXIST %CACHED_NUGET% goto copynuget
echo Downloading latest version of NuGet.exe...
IF NOT EXIST %LocalAppData%\NuGet md %LocalAppData%\NuGet
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "$ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/%NUGET_VERSION%/nuget.exe' -OutFile '%CACHED_NUGET%'"

:copynuget
IF EXIST tools\nuget\nuget.exe goto installGitLink
md tools\nuget
copy %CACHED_NUGET% tools\nuget\nuget.exe > nul


:installGitLink
IF EXIST tools\gitlink\lib\net45\GitLink.exe goto installfake
tools\nuget\nuget.exe "install" "gitlink" "-OutputDirectory" "tools" "-ExcludeVersion"


:installfake
If Exist tools\FAKE\tools\fake.exe goto build
tools\nuget\nuget.exe "install" "FAKE" "-OutputDirectory" "tools" "-ExcludeVersion"

:build
"tools\FAKE\tools\Fake.exe" build.fsx %*

