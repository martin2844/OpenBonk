#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
namespace Rewired
{
	public interface IFlightPedalsTemplate : IControllerTemplate
	{
		IControllerTemplateAxis leftPedal { get; }

		IControllerTemplateAxis rightPedal { get; }

		IControllerTemplateAxis slide { get; }
	}
}

#endif
