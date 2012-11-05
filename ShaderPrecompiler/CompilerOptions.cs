using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShaderPrecompiler
{
    class CompilerOptions
    {
        public bool ForceBuild;
        public bool CleanBuild;
        public string CompilerPath;
        public bool Debug;
        public bool CanGeneratePDBs;
        public string InputDir;
        public string ShaderModelVersion;
    }
}
