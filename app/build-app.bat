@echo off
cd /d "%~dp0\.."
set "WPFDIR=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF"
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /out:"Octane Studio MCP Setup.exe" /win32icon:"app\app.ico" /r:"%WPFDIR%\PresentationFramework.dll" /r:"%WPFDIR%\PresentationCore.dll" /r:"%WPFDIR%\WindowsBase.dll" /r:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Xaml.dll" "app\SetupApp.cs"
echo Built "Octane Studio MCP Setup.exe" successfully.
