@echo off
REM Build script for the CLI version of RB4InstrumentMapper

echo Building RB4InstrumentMapper CLI version...
echo.

REM Find MSBuild or use dotnet build
where /q msbuild
if %ERRORLEVEL% == 0 (
    echo Using MSBuild to build the CLI version...
    echo Cleaning before build...
    msbuild Program\RB4InstrumentMapper.csproj /t:Clean /p:Configuration=CLI /p:Platform=x64
    echo Building CLI version...
    msbuild Program\RB4InstrumentMapper.csproj /p:Configuration=CLI /p:Platform=x64
) else (
    echo Using dotnet build to build the CLI version...
    echo Cleaning before build...
    dotnet clean Program\RB4InstrumentMapper.csproj -c CLI -p:Platform=x64
    echo Building CLI version...
    dotnet build Program\RB4InstrumentMapper.csproj -c CLI -p:Platform=x64
)

if %ERRORLEVEL% == 0 (
    echo.
    echo Build completed successfully!
    echo The CLI executable is located at: Program\bin\x64\CLI\net472\RB4InstrumentMapperCLI.exe
    echo.
    echo You can run it using: run_mapper_cli.bat
) else (
    echo.
    echo Build failed with error code: %ERRORLEVEL%
    echo.
    echo Please check the error messages above.
)

pause 