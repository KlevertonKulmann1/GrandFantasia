@echo off
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set ASSETS=%~dp0assets
"%CSC%" /nologo /target:winexe /optimize+ /win32icon:"%~dp0app.ico" /out:"%~dp0..\GFLauncher.exe" /r:System.Web.Extensions.dll ^
  /resource:"%ASSETS%\launcher.png",GFLauncher.assets.launcher.png ^
  /resource:"%ASSETS%\btn-disabled.png",GFLauncher.assets.btn-disabled.png ^
  /resource:"%ASSETS%\btn-enabled.png",GFLauncher.assets.btn-enabled.png ^
  /resource:"%ASSETS%\btn-hover.png",GFLauncher.assets.btn-hover.png ^
  /resource:"%ASSETS%\btn-verify.png",GFLauncher.assets.btn-verify.png ^
  /resource:"%ASSETS%\btn-options.png",GFLauncher.assets.btn-options.png ^
  /resource:"%ASSETS%\btn-register.png",GFLauncher.assets.btn-register.png ^
  /resource:"%ASSETS%\close.png",GFLauncher.assets.close.png ^
  /resource:"%ASSETS%\close-hover.png",GFLauncher.assets.close-hover.png ^
  "%~dp0GFLauncher.cs"
if %errorlevel% neq 0 (
    echo ERRO ao compilar GFLauncher.exe!
    pause
    exit /b %errorlevel%
)
echo GFLauncher.exe compilado com sucesso!
pause
