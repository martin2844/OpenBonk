#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;

namespace Rewired
{
	public sealed class GamepadTemplate : ControllerTemplate, IGamepadTemplate, IControllerTemplate
	{
		public static readonly Guid typeGuid;

		public const int elementId_leftStickX = 0;

		public const int elementId_leftStickY = 1;

		public const int elementId_rightStickX = 2;

		public const int elementId_rightStickY = 3;

		public const int elementId_actionBottomRow1 = 4;

		public const int elementId_a = 4;

		public const int elementId_actionBottomRow2 = 5;

		public const int elementId_b = 5;

		public const int elementId_actionBottomRow3 = 6;

		public const int elementId_c = 6;

		public const int elementId_actionTopRow1 = 7;

		public const int elementId_x = 7;

		public const int elementId_actionTopRow2 = 8;

		public const int elementId_y = 8;

		public const int elementId_actionTopRow3 = 9;

		public const int elementId_z = 9;

		public const int elementId_leftShoulder1 = 10;

		public const int elementId_leftBumper = 10;

		public const int elementId_leftShoulder2 = 11;

		public const int elementId_leftTrigger = 11;

		public const int elementId_rightShoulder1 = 12;

		public const int elementId_rightBumper = 12;

		public const int elementId_rightShoulder2 = 13;

		public const int elementId_rightTrigger = 13;

		public const int elementId_center1 = 14;

		public const int elementId_back = 14;

		public const int elementId_center2 = 15;

		public const int elementId_start = 15;

		public const int elementId_center3 = 16;

		public const int elementId_guide = 16;

		public const int elementId_leftStickButton = 17;

		public const int elementId_rightStickButton = 18;

		public const int elementId_dPadUp = 19;

		public const int elementId_dPadRight = 20;

		public const int elementId_dPadDown = 21;

		public const int elementId_dPadLeft = 22;

		public const int elementId_leftStick = 23;

		public const int elementId_rightStick = 24;

		public const int elementId_dPad = 25;

		IControllerTemplateButton IGamepadTemplate.actionBottomRow1 => null;

		IControllerTemplateButton IGamepadTemplate.a => null;

		IControllerTemplateButton IGamepadTemplate.actionBottomRow2 => null;

		IControllerTemplateButton IGamepadTemplate.b => null;

		IControllerTemplateButton IGamepadTemplate.actionBottomRow3 => null;

		IControllerTemplateButton IGamepadTemplate.c => null;

		IControllerTemplateButton IGamepadTemplate.actionTopRow1 => null;

		IControllerTemplateButton IGamepadTemplate.x => null;

		IControllerTemplateButton IGamepadTemplate.actionTopRow2 => null;

		IControllerTemplateButton IGamepadTemplate.y => null;

		IControllerTemplateButton IGamepadTemplate.actionTopRow3 => null;

		IControllerTemplateButton IGamepadTemplate.z => null;

		IControllerTemplateButton IGamepadTemplate.leftShoulder1 => null;

		IControllerTemplateButton IGamepadTemplate.leftBumper => null;

		IControllerTemplateAxis IGamepadTemplate.leftShoulder2 => null;

		IControllerTemplateAxis IGamepadTemplate.leftTrigger => null;

		IControllerTemplateButton IGamepadTemplate.rightShoulder1 => null;

		IControllerTemplateButton IGamepadTemplate.rightBumper => null;

		IControllerTemplateAxis IGamepadTemplate.rightShoulder2 => null;

		IControllerTemplateAxis IGamepadTemplate.rightTrigger => null;

		IControllerTemplateButton IGamepadTemplate.center1 => null;

		IControllerTemplateButton IGamepadTemplate.back => null;

		IControllerTemplateButton IGamepadTemplate.center2 => null;

		IControllerTemplateButton IGamepadTemplate.start => null;

		IControllerTemplateButton IGamepadTemplate.center3 => null;

		IControllerTemplateButton IGamepadTemplate.guide => null;

		IControllerTemplateThumbStick IGamepadTemplate.leftStick => null;

		IControllerTemplateThumbStick IGamepadTemplate.rightStick => null;

		IControllerTemplateDPad IGamepadTemplate.dPad => null;

		public GamepadTemplate(object payload)
			: base(null)
		{
		}
	}
}

#endif
