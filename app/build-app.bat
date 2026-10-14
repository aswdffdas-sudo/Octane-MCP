@echo off
cd /d "%~dp0\.."
git archive --format=zip --output="app\payload.zip" HEAD MCPBridge2021.lua server.js package.json bridge.py config.json start.bat README.md SETUP.txt CHANGELOG.md extension
set "NETDIR=C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
set "WPFDIR=%NETDIR%\WPF"
"%NETDIR%\csc.exe" /nologo /target:winexe /out:"Octane Studio MCP Setup.exe" /win32icon:"app\app.ico" /resource:"app\payload.zip",OctaneMCP.Payload.zip /r:"%WPFDIR%\PresentationFramework.dll" /r:"%WPFDIR%\PresentationCore.dll" /r:"%WPFDIR%\WindowsBase.dll" /r:"%NETDIR%\System.Xaml.dll" /r:"%NETDIR%\System.IO.Compression.dll" "app\SetupApp.cs"
echo Built self-contained "Octane Studio MCP Setup.exe" successfully.
