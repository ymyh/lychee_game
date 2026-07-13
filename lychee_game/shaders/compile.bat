@echo off
REM Compile shaders to SPIR-V (Vulkan) and DXIL (D3D12)
REM Requires Vulkan SDK installed (provides glslangValidator, glslc, dxc)

echo Compiling shaders...

echo [SPIR-V] Vertex shader...
glslangValidator -V default.vert -o default_vert.spv
if %errorlevel% neq 0 (
    echo Failed to compile vertex shader to SPIR-V!
    pause
    exit /b 1
)

echo [SPIR-V] Fragment shader...
glslangValidator -V default.frag -o default_frag.spv
if %errorlevel% neq 0 (
    echo Failed to compile fragment shader to SPIR-V!
    pause
    exit /b 1
)

echo [DXIL] Vertex shader...
dxc -T vs_6_0 -E main default.vert.hlsl -Fo default_vert.dxil
if %errorlevel% neq 0 (
    echo Failed to compile vertex shader to DXIL!
    pause
    exit /b 1
)

echo [DXIL] Fragment shader...
dxc -T ps_6_0 -E main default.frag.hlsl -Fo default_frag.dxil
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
