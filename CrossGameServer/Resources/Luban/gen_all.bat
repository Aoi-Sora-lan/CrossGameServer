set SERVER_ROOT=..\..\Luban\Gen
set LUBAN_DLL=.\Tools\Luban\Luban.dll
set CONF_ROOT=.

dotnet %LUBAN_DLL% ^
    -t all ^
    -d json ^
    -c cs-newtonsoft-json ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=%SERVER_ROOT% ^
    -x outputDataDir=..\Data

pause