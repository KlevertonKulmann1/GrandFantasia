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
  /resource:"%ASSETS%\Subtract.png",GFLauncher.assets.Subtract.png ^
  /resource:"%ASSETS%\tab-display-active.png",GFLauncher.assets.tab-display-active.png ^
  /resource:"%ASSETS%\tab-display-hover.png",GFLauncher.assets.tab-display-hover.png ^
  /resource:"%ASSETS%\tab-display-off.png",GFLauncher.assets.tab-display-off.png ^
  /resource:"%ASSETS%\tab-audio-active.png",GFLauncher.assets.tab-audio-active.png ^
  /resource:"%ASSETS%\tab-audio-hover.png",GFLauncher.assets.tab-audio-hover.png ^
  /resource:"%ASSETS%\tab-audio-off.png",GFLauncher.assets.tab-audio-off.png ^
  /resource:"%ASSETS%\check-on.png",GFLauncher.assets.check-on.png ^
  /resource:"%ASSETS%\check-off.png",GFLauncher.assets.check-off.png ^
  /resource:"%ASSETS%\selector-close.png",GFLauncher.assets.selector-close.png ^
  /resource:"%ASSETS%\selector-open.png",GFLauncher.assets.selector-open.png ^
  /resource:"%ASSETS%\slider-frame.png",GFLauncher.assets.slider-frame.png ^
  /resource:"%ASSETS%\slider-progress.png",GFLauncher.assets.slider-progress.png ^
  /resource:"%ASSETS%\slider-thumb.png",GFLauncher.assets.slider-thumb.png ^
  /resource:"%ASSETS%\btn-cancel-default.png",GFLauncher.assets.btn-cancel-default.png ^
  /resource:"%ASSETS%\btn-cancel-hover.png",GFLauncher.assets.btn-cancel-hover.png ^
  /resource:"%ASSETS%\btn-cancel-off.png",GFLauncher.assets.btn-cancel-off.png ^
  /resource:"%ASSETS%\btn-save-default.png",GFLauncher.assets.btn-save-default.png ^
  /resource:"%ASSETS%\btn-save-hover.png",GFLauncher.assets.btn-save-hover.png ^
  /resource:"%ASSETS%\btn-save-off.png",GFLauncher.assets.btn-save-off.png ^
  /resource:"%ASSETS%\scroll-background.png",GFLauncher.assets.scroll-background.png ^
  /resource:"%ASSETS%\scroll-thumb.png",GFLauncher.assets.scroll-thumb.png ^
  "%~dp0GFLauncher.cs"
if %errorlevel% neq 0 (
    echo ERRO ao compilar GFLauncher.exe!
    pause
    exit /b %errorlevel%
)
echo GFLauncher.exe compilado com sucesso!
pause
