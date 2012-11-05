shader-precompiler
==================

A c# program to help make compiling HLSL shaders fast and easy. shader-precompiler keeps track of the last-modified dates of all the shaders in a specified directory (including those linked through #import statements) and ensures only those shaders that have changed should be recompiled.

**NOTE**: To run shader-precompiler you will need the microsoft shader compiler (fxc.exe) installed on your system. If you have Visual Studio 2012, or you have installed the DirectX SDK, then you already have fxc and it should be found automatically by the program.

The compiler also uses the following naming convention to determine what type of shader to compile an hlsl file as:

 * **pixel shader**: *_ps.hlsl
 * **vertex shader**: *_vs.hlsl
 * **geometry shader**: *_gs.hlsl
 * **hull shader**: *_hs.hlsl
 * **domain shader**: *_ds.hlsl

**NOTE**: All Compiled shaders will have the extension **.cso** (and if -debug was specified a .pdb file will also be created)

**NOTE**: .fx effect files are not supported

Command line parameters
-----------------------
 * **-input**:"path_to_shaders_directory" **[optional]** which directory contains the shaders to be compiled (the compiler searches recursively into this directory). If not specified, the current directory is used.
 * **-force** **[optional]** all shaders be rebuilt even if they seem up to date
 * **-clean** **[optional]** all compiled .cso and .pdb objects in the input directory be removed before compiling
 * **-debug** **[optional]** optimizations will be disabled in the compiled shaders (if omitted, optimizations will be enabled)
 * **-version** **[optional]** specify the shader model version to use when compiling (defaults to 5_0 if not specified)
 * **-compiler**:"path_to_fxc" **[optional]** specify the location on disk where the FXC compiler is located. If not specified, the following paths will be tried
   * The default install path for the windows 8 SDK.
   * The default install path for the June 2010 DirectX SDK
   * If its still not found, it is assumed that fxc can be found on the PATH

Example usage
------------

Compiling all the hlsl shaders in "c:\dev\shaders" using shader model 4 with debug symbols 
```shaderPrecompiler.exe -input:"c:\dev\shaders" -debug -version:4_0```



