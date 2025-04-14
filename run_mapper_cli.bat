@echo off
REM RB4InstrumentMapper CLI Launcher for Clone Hero Arcade Cabinet
REM ===============================================================

REM Change to the directory where this script is located
cd /d "%~dp0"

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
echo Press Ctrl+C to stop the mapping process and exit
echo.

REM Launch the CLI application
Program\bin\x64\CLI\RB4InstrumentMapperCLI.exe --mode %MODE% --wait-for-devices %WAIT_TIMEOUT% --log-file %LOG_FILE% --accurate-drums --verbose

REM If we get here, check if the application exited with an error
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo RB4InstrumentMapper exited with error code: %ERRORLEVEL%
    echo Check the log file for details.
    echo.
    pause
    exit /b %ERRORLEVEL%
)

exit /b 0 