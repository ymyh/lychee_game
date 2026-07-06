@echo off
REM Compile GLSL shaders to SPIR-V using glslangValidator
REM Requires Vulkan SDK installed

echo Compiling shaders...

glslangValidator -V default.vert -o default_vert.spv
if %errorlevel% neq 0 (
    echo Failed to compile vertex shader!
    pause
    exit /b 1
)

glslangValidator -V default.frag -o default_frag.spv
if %errorlevel% neq 0 (
    echo Failed to compile fragment shader!
    pause
    exit /b 1
)

echo Shaders compiled successfully.
echo Output: default_vert.spv, default_frag.spv
pause
