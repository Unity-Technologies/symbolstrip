A tool for removing symbol information from .NET Assemblies.

### Example OSX

>mono symbolstrip.exe MyAssembly1.dll MyAssembly2.dll

### Example Windows

>symbolstrip.exe MyAssembly1.dll MyAssembly2.dll

### What the tool does

For assemblies with native pdb, portable pdb, or mdb symbols, the symbol file will be deleted.

For assemblies with embedded pdb information, the assembly will be resaved without that information.
