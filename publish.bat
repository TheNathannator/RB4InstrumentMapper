@echo off

echo Cleaning old build outputs
dotnet clean "-p:Configuration=Debug;Platform=x64" -verbosity:minimal -consoleloggerparameters:NoSummary
if %ERRORLEVEL% NEQ 0 goto error
dotnet clean "-p:Configuration=Release;Platform=x64" -verbosity:minimal -consoleloggerparameters:NoSummary
if %ERRORLEVEL% NEQ 0 goto error
if exist "%~dp0\publish\*" (
    del /q "%~dp0\publish\*"
    if %ERRORLEVEL% NEQ 0 goto error
)
echo.

REM Build projects

REM echo Building standalone CLI
REM dotnet build RB4InstrumentMapper.CLI\RB4InstrumentMapper.CLI.csproj "-p:Configuration=Release;Platform=x64" -verbosity:minimal -consoleloggerparameters:NoSummary
REM if %ERRORLEVEL% NEQ 0 goto error
REM echo.

REM echo Building standalone GUI
REM dotnet build RB4InstrumentMapper.GUI\RB4InstrumentMapper.GUI.csproj "-p:Configuration=Release;Platform=x64" -verbosity:minimal -consoleloggerparameters:NoSummary
REM if %ERRORLEVEL% NEQ 0 goto error
REM echo.

echo Building CLI+GUI installer
dotnet build RB4InstrumentMapper.Installer\RB4InstrumentMapper.Installer.wixproj "-p:Configuration=Release;Platform=x64" -verbosity:minimal -consoleloggerparameters:NoSummary
if %ERRORLEVEL% NEQ 0 goto error
echo.

REM Copy files and build archives

REM echo Packaging standalone CLI
REM 7z a "%~dp0\publish\RB4InstrumentMapper.CLI-x64.zip" "%~dp0\RB4InstrumentMapper.CLI\bin\x64\Release\net472" -bb3 -bd -sse > nul
REM if %ERRORLEVEL% NEQ 0 goto error
REM 7z rn "%~dp0\publish\RB4InstrumentMapper.CLI-x64.zip" "net472" "RB4InstrumentMapper.CLI" -bb0 -bd > nul
REM if %ERRORLEVEL% NEQ 0 goto error
REM echo.

REM echo Packaging standalone GUI
REM 7z a "%~dp0\publish\RB4InstrumentMapper.GUI-x64.zip" "%~dp0\RB4InstrumentMapper.GUI\bin\x64\Release\net472" -bb3 -bd -sse > nul
REM if %ERRORLEVEL% NEQ 0 goto error
REM 7z rn "%~dp0\publish\RB4InstrumentMapper.GUI-x64.zip" "net472" "RB4InstrumentMapper.GUI" -bb0 -bd > nul
REM if %ERRORLEVEL% NEQ 0 goto error
REM echo.

echo Copying installer package
copy "%~dp0\RB4InstrumentMapper.Installer\bin\x64\Release\RB4InstrumentMapper.Installer.exe" "%~dp0\publish\RB4InstrumentMapperInstaller-x64.exe"
if %ERRORLEVEL% NEQ 0 goto error

exit

:error
echo.
echo Build failed.
exit
