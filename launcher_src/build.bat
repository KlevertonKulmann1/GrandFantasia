@echo off
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
"%CSC%" /nologo /target:winexe /optimize+ /out:"%~dp0..\GFLauncher.exe" /r:System.Web.Extensions.dll "%~dp0GFLauncher.cs"
if %errorlevel% neq 0 (
    echo ERRO ao compilar GFLauncher.exe!
    pause
    exit /b %errorlevel%
)
echo GFLauncher.exe compilado com sucesso!
pause
