using System;
using Mono.Cecil;

internal static class NormalizeAndroidManagedAssembly
{
	private static int Main(string[] args)
	{
		if (args.Length != 3)
		{
			Console.Error.WriteLine("Usage: NormalizeAndroidManagedAssembly <source.dll> <output.dll> <unity-mono-assemblies>");
			return 2;
		}

		var resolver = new DefaultAssemblyResolver();
		resolver.AddSearchDirectory(args[2]);
		resolver.AddSearchDirectory(System.IO.Path.Combine(args[2], "Facades"));
		var assembly = AssemblyDefinition.ReadAssembly(args[0], new ReaderParameters { ReadWrite = false, AssemblyResolver = resolver });
		assembly.MainModule.Kind = ModuleKind.Dll;
		assembly.Write(args[1]);
		Console.WriteLine("Normalized " + assembly.Name.Name + " as a managed DLL: " + args[1]);
		return 0;
	}
}
