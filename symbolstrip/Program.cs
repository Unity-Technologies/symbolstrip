using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;

namespace symbolstrip
{
    internal class Program
    {
        private static List<string> filesToDelete = new List<string>();
        private static List<Tuple<string, string>> assembliesWithEmbeddedRemoved = new List<Tuple<string, string>>();
        public static void Main(string[] args)
        {
            foreach(var file in args)
                RemoveSymbols(file);

            foreach (var fileToDelete in filesToDelete)
            {
                try
                {
                    File.Delete(fileToDelete);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to delete symbol file : {fileToDelete}");
                }
            }

            foreach (var item in assembliesWithEmbeddedRemoved)
            {
                try
                {
                    File.Delete(item.Item1);
                }
                catch (Exception)
                {
                    Console.WriteLine($"Failed to delete original assembly : {item.Item1}");
                    continue;
                }
                
                File.Move(item.Item2, item.Item1);
            }
        }

        static void RemoveSymbols(string fileName)
        {
            var readerParameters = new ReaderParameters
            {
                SymbolReaderProvider = new DefaultSymbolReaderProvider(false)
            };

            using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(fileName, readerParameters))
            {

                if (assemblyDefinition.MainModule.SymbolReader == null)
                    return;

                try
                {
                    if (assemblyDefinition.MainModule.SymbolReader is NativePdbReader
                        || assemblyDefinition.MainModule.SymbolReader is PortablePdbReader)
                    {
                        // Can't delete now because the file will be open.  
                        var symbolFilePath = Path.ChangeExtension(assemblyDefinition.MainModule.FileName, "pdb");
                        filesToDelete.Add(symbolFilePath);
                        return;
                    }

                    if (assemblyDefinition.MainModule.SymbolReader is MdbReader)
                    {
                        var symbolFilePath = $"{assemblyDefinition.MainModule.FileName}.mdb";
                        filesToDelete.Add(symbolFilePath);
                        return;
                    }

                    if (assemblyDefinition.MainModule.SymbolReader is EmbeddedPortablePdbReader)
                    {
                        var cleanPath = AddPostFixToFileName(assemblyDefinition.MainModule.FileName, "-clean");
                        
                        if(File.Exists(cleanPath))
                            File.Delete(cleanPath);
                        
                        assemblyDefinition.Write(cleanPath, new WriterParameters());
                        assembliesWithEmbeddedRemoved.Add(new Tuple<string, string>(assemblyDefinition.MainModule.FileName, cleanPath));
                        return;
                    }
                    
                    throw new InvalidOperationException($"Unhandled read type of {assemblyDefinition.MainModule.SymbolReader}");
                }
                finally 
                {
                    assemblyDefinition.MainModule.SymbolReader.Dispose();
                }
            }
        }

        static string AddPostFixToFileName(string filePath, string postfix)
        {
            var dir = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var newName = $"{fileName}{postfix}{Path.GetExtension(filePath)}";
            if (dir == null)
                return newName;
            return Path.Combine(dir, newName);
        }
    }
}