using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ShaderPrecompiler
{
    class Compiler
    {
        private static Regex IncludeRegex = new Regex("#include\\s*\"(.*)\"", RegexOptions.Compiled);
        private CompilerOptions _options;

        public Compiler(CompilerOptions options)
        {
            _options = options;
        }

        public bool CompileShaders()
        {
            DirectoryInfo di = new DirectoryInfo(_options.InputDir);
            return RecursiveCompile(di, ".hlsl")==0;
        }


        private int RecursiveCompile(DirectoryInfo info, string pattern)
        {
            int failures = 0;

            if (_options.CleanBuild)
            {
                foreach (var child in info.GetFiles("*.cso"))
                {
                    var associatedPdb = child.FullName.Substring(0,child.FullName.Length - 4) + ".pdb";
                    if (_options.CanGeneratePDBs && File.Exists(associatedPdb))
                    {
                        File.Delete(associatedPdb);
                        Console.WriteLine("Cleaning - '" + associatedPdb + "'");
                    }
                    Console.WriteLine("Cleaning - '" + child.FullName + "'");
                    child.Delete();
                }
            }

            foreach (var child in info.GetFiles("*" + pattern))
            {
                //determine what type of shader we're compiling
                string compileTarget;
                if (child.Name.EndsWith("_vs" + pattern, StringComparison.OrdinalIgnoreCase))
                {
                    compileTarget = "vs";
                }
                else if (child.Name.EndsWith("_ps" + pattern, StringComparison.OrdinalIgnoreCase))
                {
                    compileTarget = "ps";
                }
                else if (child.Name.EndsWith("_gs" + pattern, StringComparison.OrdinalIgnoreCase))
                {
                    compileTarget = "gs";
                }
                else if (child.Name.EndsWith("_ds" + pattern, StringComparison.OrdinalIgnoreCase))
                {
                    compileTarget = "ds";
                }
                else if (child.Name.EndsWith("_hs" + pattern, StringComparison.OrdinalIgnoreCase))
                {
                    compileTarget = "hs";
                }
                else
                {
                    //not a known shader target, don't compile
                    Console.WriteLine(child.FullName+ "(1,1): WARNING: Unsure of shader type - skipping");
                    continue;
                }
                 
                string flags;
                if (_options.Debug)
                {
                    flags = "/Od ";
                    if (_options.CanGeneratePDBs)
                    {
                        flags += "/Zi /Fd \"" + child.FullName.Substring(0,child.FullName.Length-5) + ".pdb\"";
                    }
                }
                else
                {
                    flags = string.Empty;
                }

                string compiledTarget = child.FullName.Substring(0,child.FullName.Length - 5) + ".cso";

                ProcessStartInfo start = new ProcessStartInfo(
                    _options.CompilerPath,
                    "/nologo /T " + compileTarget + "_"+ _options.ShaderModelVersion +" /E main /Fo \"" + compiledTarget + "\" " + 
                    flags + " \"" + child.FullName + "\"") 
                    { 
                        CreateNoWindow = true, 
                        UseShellExecute = false, 
                        RedirectStandardOutput = true 
                    };

                bool precompile = true;

                //precompile shaders unless explicitly stated
                using (FileStream fs = new FileStream(child.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var reader = new StreamReader(fs))
                    {
                        if (reader.EndOfStream ||
                            reader.ReadLine().StartsWith("#pragma message \"noprecompile\"", true, CultureInfo.InvariantCulture))
                        {
                            precompile = false;
                        }
                    }
                }

                //if this shader has no precompiled pair, or its precompiled data is older than the source
                //we should compile an updated version.
                FileInfo precompiled = new FileInfo(compiledTarget);

                //if we aren't supposed to precompile this, ensure we remove any pre existing precompiled version
                if (!precompile)
                {
                    if (precompiled.Exists)
                    {
                        Console.WriteLine("Deleting - '" + precompiled.FullName + "'");
                        File.Delete(precompiled.FullName);
                    }
                }
                //otherwise if we are supposed to precompile, compile if there is no precompiled versino, or if its out
                //of date.
                else if (!precompiled.Exists || precompiled.LastWriteTimeUtc <= GetLastModified(child.FullName) || _options.ForceBuild)
                {
                    if (precompiled.Exists) File.Delete(precompiled.FullName);
                    Console.WriteLine("Compiling - '" + child.FullName + "'...");

                    using (var p = Process.Start(start))
                    {
                        if (!p.WaitForExit(30000))
                        {
                            Console.WriteLine(child.FullName + "(1,1): error - compiler timed out");
                            ++failures;
                        }
                        else
                        {
                            try
                            {
                                using (StreamReader reader = p.StandardOutput)
                                {
                                    Console.Write(reader.ReadToEnd());
                                }
                            }
                            catch (Exception)
                            {
                            }
                            if (p.ExitCode != 0) ++failures;
                        }
                    }
                }
            }
            foreach (var child in info.GetDirectories())
            {
                failures += RecursiveCompile(child, pattern);
            }
            return failures;
        }

        /**
         * find the last modified date of a shader file (recursivly checks last modified dates of any included files)
         */
        private DateTime GetLastModified(string filename)
        {
            FileInfo fi = new FileInfo(filename);
            DateTime lastModified = fi.LastWriteTimeUtc;

            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new StreamReader(fs))
                {
                    var matches = IncludeRegex.Matches(reader.ReadToEnd());

                    foreach (var match in matches)
                    {
                        var lm = GetLastModified(Path.Combine(fi.DirectoryName, ((Match)match).Groups[1].Value));
                        if (lm > lastModified) lastModified = lm;
                    }
                }
            }
            return lastModified;
        }
    }
}
