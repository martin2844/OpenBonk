using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class CreateAndroidSteamworksStub
{
	private static int Main(string[] args)
	{
		if (args.Length != 2)
		{
			Console.Error.WriteLine("Usage: CreateAndroidSteamworksStub <source.dll> <output.dll>");
			return 2;
		}

		var assembly = AssemblyDefinition.ReadAssembly(args[0], new ReaderParameters { ReadWrite = false });
		int patched = 0;
		foreach (var type in GetTypes(assembly.MainModule.Types))
		{
			foreach (var method in type.Methods.Where(m => (m.Attributes & MethodAttributes.PInvokeImpl) != 0).ToArray())
			{
				method.PInvokeInfo = null;
				method.Attributes &= ~MethodAttributes.PInvokeImpl;
				method.ImplAttributes &= ~MethodImplAttributes.InternalCall;
				method.ImplAttributes = (method.ImplAttributes & ~MethodImplAttributes.CodeTypeMask) | MethodImplAttributes.IL;
				method.Body = CreateDefaultBody(method);
				patched++;
			}
		}

		if (patched == 0)
		{
			Console.Error.WriteLine("No P/Invoke methods found; refusing to write an unchanged assembly.");
			return 1;
		}

		// AssetRipper preserved these inputs as PE executables despite their .dll
		// filenames. UnityLinker accepts them, but ARM64 IL2CPP then searches for
		// an executable entry assembly and fails. Emit a real managed library.
		assembly.MainModule.Kind = ModuleKind.Dll;
		assembly.Write(args[1]);
		Console.WriteLine("Stubbed " + patched + " P/Invoke methods in " + args[1]);
		return 0;
	}

	private static MethodBody CreateDefaultBody(MethodDefinition method)
	{
		var body = new MethodBody(method) { InitLocals = true };
		var processor = body.GetILProcessor();
		var returnType = method.ReturnType;
		if (returnType.MetadataType == MetadataType.Void)
		{
			processor.Emit(OpCodes.Ret);
			return body;
		}

		if (returnType.IsValueType || returnType.IsGenericParameter)
		{
			var value = new VariableDefinition(returnType);
			body.Variables.Add(value);
			processor.Emit(OpCodes.Ldloca, value);
			processor.Emit(OpCodes.Initobj, returnType);
			processor.Emit(OpCodes.Ldloc, value);
		}
		else
		{
			processor.Emit(OpCodes.Ldnull);
		}

		processor.Emit(OpCodes.Ret);
		return body;
	}

	private static System.Collections.Generic.IEnumerable<TypeDefinition> GetTypes(
		System.Collections.Generic.IEnumerable<TypeDefinition> types)
	{
		foreach (var type in types)
		{
			yield return type;
			foreach (var nested in GetTypes(type.NestedTypes))
				yield return nested;
		}
	}
}
