@echo off

"%ProgramFiles%\Unity\Editor\unity.exe" -batchmode -builtTarget win64 -buildWindows64Player "%TEMP%\AITD room viewer.exe"  -quit
if %ERRORLEVEL% NEQ 0 pause

"%PROGRAMFILES%\7-Zip\7z" a -tzip "AITD_room_viewer.zip" ^
 "%TEMP%\AITD room viewer_Data" ^
 "%TEMP%\AITD room viewer.exe" ^
 "-mx=9"
if %ERRORLEVEL% NEQ 0 pause

rd /s/q "AITD room viewer_Data"
del "AITD room viewer.exe"
