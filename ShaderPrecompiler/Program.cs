using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ShaderPrecompiler
{
    /// <summary>
    /// The compiler automatically includes all .hlsl files included in the input directory. .fx effect files are not supported
    /// 
    /// Put the following at the start of a hlsl if you want it to be exlcuded from compilation
    /// #pragma message "noprecompile"
    /// 
    /// The compiler also uses the following naming convention to determine what type of shader to compile an hlsl file as
    /// 
    /// pixel shader: *_ps.hlsl
    /// vertex shader: *_vs.hlsl
    /// geometry shader: *_gs.hlsl
    /// hull shader: *_hs.hlsl
    /// domain shader: *_ds.hlsl
    /// 
    /// command line arguments
    /// -input:"path_to_shaders_directory" [optional] which directory contains the shaders to be compiled (the compiler searches recursively into this directory)
    ///                                     If not specified, the current directory is used.
    /// -force [optional] all shaders be rebuilt even if they seem up to date
    /// -clean [optional] all compiled .cso and .pdb objects in the input directory be removed before compiling
    /// -debug [optional] optimizations will be disabled in the compiled shaders (if omitted, optimizations will be enabled)
    /// -version [optional] specify the shader model version to use when compiling (defaults to 5_0 if not specified)
    /// -compiler:"path_to_fxc" [optional] specify the location on disk where the FXC compiler is located. If not specified, the following paths will be tried
    ///                                     The default install path for the windows 8 SDK.
    ///                                     The default install path for the June 2010 DirectX SDK
    ///                                     If its still not found, it is assumed that fxc can be found on the PATH
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            CommandLineParser parsedArguments;
            try
            {
                parsedArguments = new CommandLineParser(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Failed to parse command line arguments "+ ex);
                return 1;
            }

            CompilerOptions options = new CompilerOptions();
            LocateCompiler(parsedArguments, options);
            options.Debug = !string.IsNullOrEmpty(parsedArguments["debug"]);
            options.ForceBuild = !string.IsNullOrEmpty(parsedArguments["force"]);
            options.CleanBuild =  !string.IsNullOrEmpty(parsedArguments["clean"]);

            if (string.IsNullOrEmpty(parsedArguments["version"]))
            {
                options.ShaderModelVersion = "5_0";
            }
            else
            {
                options.ShaderModelVersion = parsedArguments["version"];
            }

            if (!string.IsNullOrEmpty(parsedArguments["input"]))
            {
                options.InputDir = parsedArguments["input"];
            }
            else
            {
                Console.WriteLine("WARNING: no input directory specified, assuming current directory...");
                options.InputDir = Directory.GetCurrentDirectory();
            }

            if (!Directory.Exists(options.InputDir))
            {
                Console.WriteLine("ERROR: Input directory " + options.InputDir +" doesn't exist");
                return 1;
            }


            Compiler compiler = new Compiler(options);
            compiler.CompileShaders();

            if (compiler.CompileShaders())
            {
                Console.WriteLine("All shaders compiled successfully.");
                return 0;
            }
            else
            {
                Console.WriteLine("ERROR: some shaders failed to compile.");
                return 1;
            }
        }

        private static void LocateCompiler(CommandLineParser parsedArguments, CompilerOptions options)
        {
            string windows8SDKPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\Windows Kits\\8.0\\bin\\x86\\fxc.exe";
            string DXSDKPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\Microsoft DirectX SDK (June 2010)\\Utilities\\bin\\x86\\fxc.exe";

            //we will give the windows 8 SDK compiler preference 
            if (!string.IsNullOrEmpty(parsedArguments["compiler"]))
            {
                if (File.Exists(parsedArguments["compiler"]))
                {
                    options.CompilerPath = parsedArguments["compiler"];
                }
                else
                {
                    Console.WriteLine("WARNING: Specified compiler not found at " + parsedArguments["compiler"] + " attempting to locate from default paths...");
                }
            }

            if (string.IsNullOrEmpty(options.CompilerPath))
            {
                if (File.Exists(windows8SDKPath))
                {
                    Console.WriteLine("Using fxc from windows 8 SDK...");
                    options.CompilerPath = windows8SDKPath;
                }
                //then we'll try to use the compiler in the DX June 2012 SDK
                else if (File.Exists(DXSDKPath))
                {
                    Console.WriteLine("Using fxc from June 2010 DX SDK...");
                    options.CompilerPath = DXSDKPath;
                }
                //couldn't find the compiler, so lets hope its on the path
                else
                {
                    Console.WriteLine("WARNING: Couldn't find fxc, assuming its on the PATH...");
                    options.CompilerPath = "fxc.exe";
                }
            }

            try
            {
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(options.CompilerPath);
                if (fvi.FileMajorPart >= 9 && fvi.FileMinorPart >= 30)
                {
                    options.CanGeneratePDBs = true;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("WARNING: Couldn't determine if fxc supports pdb file generation...");
                options.CanGeneratePDBs = false;
            }
        }
    }
}
