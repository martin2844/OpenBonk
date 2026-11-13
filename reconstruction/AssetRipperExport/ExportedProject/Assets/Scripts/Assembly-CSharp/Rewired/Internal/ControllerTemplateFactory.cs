#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;

namespace Rewired.Internal
{
	public static class ControllerTemplateFactory
	{
		private static readonly Type[] _defaultTemplateTypes;

		private static readonly Type[] _defaultTemplateInterfaceTypes;

		public static Type[] templateTypes => null;

		public static Type[] templateInterfaceTypes => null;

		public static IControllerTemplate Create(Guid typeGuid, object payload)
		{
			return null;
		}
	}
}

#endif
