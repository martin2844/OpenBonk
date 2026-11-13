using System;

public class DebugCommand : DebugCommandBase
{
	private Action command;

	public DebugCommand(string id, string description, string format, Type commandType, Action command)
		: base(null, null, null, null)
	{
	}

	public void Invoke()
	{
	}
}
public class DebugCommand<T> : DebugCommandBase
{
	private Action<T> command;

	public DebugCommand(string id, string description, string format, Type commandType, Action<T> command)
		: base(null, null, null, null)
	{
	}

	public void Invoke(T value)
	{
	}
}
