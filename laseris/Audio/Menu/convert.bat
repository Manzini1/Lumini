@echo off
setlocal

set "FFMPEG=E:\Downloads\ffmpeg-8.0.1-full_build\ffmpeg-8.0.1-full_build\bin\ffmpeg.exe"

for %%f in (*.mp3) do (
  echo Convertendo: %%f
  "%FFMPEG%" -y -i "%%f" -c:a libvorbis -q:a 6 "%%~nf.ogg"
)

echo.
echo Terminado!
pause
