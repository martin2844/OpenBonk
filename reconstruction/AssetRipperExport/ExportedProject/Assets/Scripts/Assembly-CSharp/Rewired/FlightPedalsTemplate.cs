#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;

namespace Rewired
{
	public sealed class FlightPedalsTemplate : ControllerTemplate, IFlightPedalsTemplate, IControllerTemplate
	{
		public static readonly Guid typeGuid;

		public const int elementId_leftPedal = 0;

		public const int elementId_rightPedal = 1;

		public const int elementId_slide = 2;

		IControllerTemplateAxis IFlightPedalsTemplate.leftPedal => null;

		IControllerTemplateAxis IFlightPedalsTemplate.rightPedal => null;

		IControllerTemplateAxis IFlightPedalsTemplate.slide => null;

		public FlightPedalsTemplate(object payload)
			: base(null)
		{
		}
	}
}

#endif
