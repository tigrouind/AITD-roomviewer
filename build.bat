@echo off

"%ProgramFiles%\Unity\Editor\unity.exe" -batchmode -builtTarget win64 -buildWindows64Player "%TEMP%\AITD room viewer.exe" -quit
if %ERRORLEVEL% NEQ 0 pause

md "%TEMP%\GAMEDATA"
copy /y ".\GAMEDATA\vars.txt" "%TEMP%\GAMEDATA"

"%PROGRAMFILES%\7-Zip\7z" a -tzip "AITD_room_viewer.zip" ^
 "%TEMP%\AITD room viewer_Data" ^
 "%TEMP%\AITD room viewer.exe" ^
 "%TEMP%\GAMEDATA" ^
 "-mx=9"
if %ERRORLEVEL% NEQ 0 pause

rd /s/q "%TEMP%\GAMEDATA"
rd /s/q "%TEMP%\AITD room viewer_Data"
del "%TEMP%\AITD room viewer.exe"
