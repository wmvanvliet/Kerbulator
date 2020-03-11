@echo off

REM Location of your KSP installation
SET KSPDIR=C:\Program Files (x86)\Steam\SteamApps\common\Kerbal Space Program

REM Location of the Unity assembly files inside your KSP installation
SET MANAGED=KSP_x64_Data\Managed

REM This should be the Roslyn .NET compiler
SET MCS=C:\Users\wmvan\Microsoft.Net.Compilers.3.4.0\tools\csc.exe

REM Compile the plugin
mkdir build
@echo on
"%MCS%" -target:library -lib:"%KSPDIR%\%MANAGED%" -r:Assembly-CSharp.dll,Assembly-CSharp-firstpass.dll,UnityEngine.dll,UnityEngine.CoreModule.dll,UnityEngine.IMGUIModule.dll,UnityEngine.AnimationModule.dll,UnityEngine.InputLegacyModule.dll -out:build\Kerbulator.dll *.cs
@echo off

REM Collect the plugin files in a folder called "package\Kerbulator"
mkdir package\Kerbulator\Plugins
mkdir package\Kerbulator\Textures
copy build\Kerbulator.dll package\Kerbulator\Plugins
copy icons\*.png package\Kerbulator\Textures
copy README.md package\Kerbulator
copy LICENSE.md package\Kerbulator
mkdir package\Kerbulator\doc
copy doc\*.mkd package\Kerbulator\doc
copy doc\*.png package\Kerbulator\doc
