rem # Anyway, this file will never be used because only non-Windows OSs need an elevated shell to bind ports lower than 1024. 

@echo off
set cmd=%1
set out=%2

rem echo %cmd%
rem echo %out%

start /B /wait cmd /C "%cmd% 1> %out% 2>&1"
