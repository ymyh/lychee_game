@echo off
REM Compile the Slang shader sources to SPIR-V (Vulkan) and DXIL (D3D12).
REM Requires Slang installed with slangc on PATH.
REM SPIR-V goes through Slang's GLSL backend to stay at SPIR-V 1.0 for wide device support.
REM DXIL needs a matching dxc/dxil.dll pair: an older dxc can pick up a newer dxil.dll
REM from PATH and reject valid output, so the Windows SDK pair is preferred here.

REM Prefer the Windows SDK dxc/dxil.dll pair when present.
set "WINSDK_X64="
for %%R in ("%ProgramFiles(x86)%\Windows Kits\10\bin" "E:\Windows Kits\10\bin") do (
    for /d %%D in ("%%~R\10.0.*") do (
        if exist "%%~fD\x64\dxcompiler.dll" set "WINSDK_X64=%%~fD\x64"
    )
)
if defined WINSDK_X64 set "PATH=%WINSDK_X64%;%PATH%"

echo Compiling shaders...

echo [SPIR-V] Vertex shader...
slangc default.slang -target spirv -entry vertexMain -stage vertex -emit-spirv-via-glsl -o default_vert.spv
if %errorlevel% neq 0 (
    echo Failed to compile vertex shader to SPIR-V!
    pause
    exit /b 1
)

echo [SPIR-V] Fragment shader...
slangc default.slang -target spirv -entry fragmentMain -stage fragment -emit-spirv-via-glsl -o default_frag.spv
if %errorlevel% neq 0 (
    echo Failed to compile fragment shader to SPIR-V!
    pause
    exit /b 1
)

echo [DXIL] Vertex shader...
slangc default.slang -target dxil -entry vertexMain -stage vertex -profile sm_6_0 -o default_vert.dxil
if %errorlevel% neq 0 (
    echo Failed to compile vertex shader to DXIL!
    pause
    exit /b 1
)

echo [DXIL] Fragment shader...
slangc default.slang -target dxil -entry fragmentMain -stage fragment -profile sm_6_0 -o default_frag.dxil
if %errorlevel% neq 0 (
    echo Failed to compile fragment shader to DXIL!
    pause
    exit /b 1
)

echo.
echo All shaders compiled successfully.
echo   SPIR-V: default_vert.spv, default_frag.spv
echo   DXIL:   default_vert.dxil, default_frag.dxil
pause
