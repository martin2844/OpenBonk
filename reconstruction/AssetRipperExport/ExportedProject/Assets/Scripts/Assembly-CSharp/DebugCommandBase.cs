using System;

public class DebugCommandBase
{
	public Type commandType;

	public string commandId { get; private set; }

	public string commandDescription { get; private set; }

	public string commandFormat { get; private set; }

	public DebugCommandBase(string id, string description, string format, Type commandType)
	{
	}
}
