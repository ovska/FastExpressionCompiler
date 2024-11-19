@echo off
setlocal EnableDelayedExpansion

set LEGACYCI=""
set TF=""
if [%1]==[--legacyci] set LEGACYCI=-p:LEGACYCI=true
if [%1]==[--legacyci] (set TF=-f net8.0) else (set TF=-f net9.0)
echo:First parameter set to: '%LEGACYCI%' amd TF is '%TF%' 

echo: 
echo:## Starting: RESTORE and BUILD...
echo: 

dotnet clean %LEGACYCI% -v:m
dotnet build %LEGACYCI% -c:Release -v:m
if %ERRORLEVEL% neq 0 goto :error

echo:
echo:## Finished: RESTORE and BUILD

echo: 
echo:## Starting: TESTS...
echo:

dotnet run %LEGACYCI% --no-build -c Release %TF% --project test/FastExpressionCompiler.TestsRunner
if %ERRORLEVEL% neq 0 goto :error

dotnet run --no-build -c Release --project test/FastExpressionCompiler.TestsRunner.Net472
if %ERRORLEVEL% neq 0 goto :error
echo:
echo:## Finished: TESTS

echo: 
echo:## Starting: SOURCE PACKAGING...
echo:
call BuildScripts\NugetPack.bat
if %ERRORLEVEL% neq 0 goto :error
echo:
echo:## Finished: SOURCE PACKAGING
echo: 
echo:# Finished: ALL
echo:
exit /b 0

:error
echo:
echo:## :-( Failed with ERROR: %ERRORLEVEL%
echo:
exit /b %ERRORLEVEL%
