echo off

REM Location of your KSP installation
SET KSPDIR=C:\Program Files (x86)\Steam\SteamApps\common\Kerbal Space Program

REM Location of the Unity assembly files inside your KSP installation
SET MANAGED=KSP_x64_Data\Managed

REM This should be mono version 3.x (newer versions of mono dropped .NET 3.5 support)
SET MCS=C:\Program Files (x86)\Mono\bin\mcs

REM Compile the plugin
mkdir build
echo on
"%MCS%" -t:library -lib:"%KSPDIR%\%MANAGED%" -r:Assembly-CSharp,Assembly-CSharp-firstpass,UnityEngine -out:build\Kerbulator.dll *.cs
echo off

REM Collect the plugin files in a folder called "package\Kerbulator"
mkdir package\Kerbulator\Plugins
mkdir package\Kerbulator\Textures
copy build\Kerbulator.dll package\Kerbulator\Plugins
copy icons\*.png package\Kerbulator\Textures
copy README.md package\Kerbulator
copy LICENSE.md package\Kerbulator
mkdir package\Kerbulator\doc
copy doc\*.mkd doc\*.png package\Kerbulator\doc
