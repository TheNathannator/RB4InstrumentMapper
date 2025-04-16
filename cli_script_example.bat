@echo off
REM RB4InstrumentMapper CLI Launcher Example
REM =======================================================
REM This example script is intended to be run inside of the
REM project source directory, with the CLI built already.

REM Change to the directory where this script is located
pushd /d "%~dp0"

echo Starting RB4InstrumentMapper in CLI mode...
echo.

REM Configuration Parameters
set MODE=vigem
set TIMEOUT=0
set WAIT_FOR_DEVICES=true
set WAIT_TIMEOUT=30
set LOG_FILE="%~dp0RB4InstrumentMapper_log.txt"

REM Run the CLI version with the specified parameters
echo Running with: --mode %MODE% --wait-for-devices %WAIT_TIMEOUT% --log-file %LOG_FILE%
echo.

REM Launch the CLI application
RB4InstrumentMapper.CLI\bin\x64\Release\net472\RB4InstrumentMapper.CLI.exe --mode %MODE% --wait-for-devices %WAIT_TIMEOUT% --log-file %LOG_FILE% --accurate-drums --verbose

REM Restore the previous working directory
popd

REM If we get here, check if the application exited with an error
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo RB4InstrumentMapper exited with error code: %ERRORLEVEL%
    echo.
    pause
    exit /b %ERRORLEVEL%
)

exit /b 0 