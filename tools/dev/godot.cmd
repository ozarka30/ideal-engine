@echo off
rem Launch the Godot editor on this project with a .NET SDK on PATH.
rem
rem Godot builds the C# project by running `dotnet` from PATH, and it has no editor setting
rem for the SDK location. A machine-wide install at C:\Program Files\dotnet always precedes
rem a user one, so if that install is runtime-only, `dotnet --list-sdks` comes back empty and
rem every .cs script fails to resolve -- opening a scene then reports its script as a missing
rem dependency. Prepending the user SDK here fixes it for the process we start, with no admin.
rem
rem   tools\dev\godot.cmd              open the editor
rem   tools\dev\godot.cmd --headless --import      or pass any Godot arguments
rem
rem Set GODOT_PATH to use a different binary. Install the SDK with:
rem   powershell -c "& ([scriptblock]::Create((irm https://dot.net/v1/dotnet-install.ps1))) -Channel 9.0"

setlocal
set "SDK=%USERPROFILE%\.dotnet"
if not exist "%SDK%\dotnet.exe" (
  echo No .NET SDK at %SDK%. Install one with:
  echo   powershell -c "^& ^([scriptblock]::Create^(^(irm https://dot.net/v1/dotnet-install.ps1^)^)^) -Channel 9.0"
  exit /b 1
)
set "PATH=%SDK%;%PATH%"
set "DOTNET_ROOT=%SDK%"

if defined GODOT_PATH (
  set "GODOT=%GODOT_PATH%"
) else (
  set "GODOT=%USERPROFILE%\.cache\companywars\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe"
)
if not exist "%GODOT%" (
  echo No Godot at %GODOT%. Set GODOT_PATH to a 4.7.2 mono binary.
  exit /b 1
)

set "PROJECT=%~dp0..\.."

rem Build first. Godot only builds the C# project on demand, and a run never does -- an
rem unbuilt assembly means every script fails to instantiate and a scene reports its script
rem as a missing dependency, which reads like a broken file rather than a missing build.
"%SDK%\dotnet.exe" build "%PROJECT%\game\CompanyWars.Game.csproj" -v quiet --nologo
if errorlevel 1 (
  echo Build failed; not starting Godot.
  exit /b 1
)

"%GODOT%" --path "%PROJECT%\game" %*
