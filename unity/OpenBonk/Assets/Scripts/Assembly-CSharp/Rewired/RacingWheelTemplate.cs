#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;

namespace Rewired
{
	public sealed class RacingWheelTemplate : ControllerTemplate, IRacingWheelTemplate, IControllerTemplate
	{
		public static readonly Guid typeGuid;

		public const int elementId_wheel = 0;

		public const int elementId_accelerator = 1;

		public const int elementId_brake = 2;

		public const int elementId_clutch = 3;

		public const int elementId_shiftDown = 4;

		public const int elementId_shiftUp = 5;

		public const int elementId_wheelButton1 = 6;

		public const int elementId_wheelButton2 = 7;

		public const int elementId_wheelButton3 = 8;

		public const int elementId_wheelButton4 = 9;

		public const int elementId_wheelButton5 = 10;

		public const int elementId_wheelButton6 = 11;

		public const int elementId_wheelButton7 = 12;

		public const int elementId_wheelButton8 = 13;

		public const int elementId_wheelButton9 = 14;

		public const int elementId_wheelButton10 = 15;

		public const int elementId_consoleButton1 = 16;

		public const int elementId_consoleButton2 = 17;

		public const int elementId_consoleButton3 = 18;

		public const int elementId_consoleButton4 = 19;

		public const int elementId_consoleButton5 = 20;

		public const int elementId_consoleButton6 = 21;

		public const int elementId_consoleButton7 = 22;

		public const int elementId_consoleButton8 = 23;

		public const int elementId_consoleButton9 = 24;

		public const int elementId_consoleButton10 = 25;

		public const int elementId_shifter1 = 26;

		public const int elementId_shifter2 = 27;

		public const int elementId_shifter3 = 28;

		public const int elementId_shifter4 = 29;

		public const int elementId_shifter5 = 30;

		public const int elementId_shifter6 = 31;

		public const int elementId_shifter7 = 32;

		public const int elementId_shifter8 = 33;

		public const int elementId_shifter9 = 34;

		public const int elementId_shifter10 = 35;

		public const int elementId_reverseGear = 44;

		public const int elementId_select = 36;

		public const int elementId_start = 37;

		public const int elementId_systemButton = 38;

		public const int elementId_horn = 43;

		public const int elementId_dPadUp = 39;

		public const int elementId_dPadRight = 40;

		public const int elementId_dPadDown = 41;

		public const int elementId_dPadLeft = 42;

		public const int elementId_dPad = 45;

		IControllerTemplateAxis IRacingWheelTemplate.wheel => null;

		IControllerTemplateAxis IRacingWheelTemplate.accelerator => null;

		IControllerTemplateAxis IRacingWheelTemplate.brake => null;

		IControllerTemplateAxis IRacingWheelTemplate.clutch => null;

		IControllerTemplateButton IRacingWheelTemplate.shiftDown => null;

		IControllerTemplateButton IRacingWheelTemplate.shiftUp => null;

		IControllerTemplateButton IRacingWheelTemplate.wheelButton1 => null;

		IControllerTemplateButton IRacingWheelTemplate.wheelButton2 => null;

		IControllerTemplateButton IRacingWheelTemplate.wheelButton3 => null;

		IControllerTemplateButton IRacingWheelTemplate.wheelButton4 => null;

		IControllerTemplateButton IRacingWheelTemplate.wheelButton5 => null;

		IControllerTemplateButton IRacingWheelTemplate.wheelButton6 => null;

		IControllerTemplateButton IRacingWheelTemplate.wheelButton7 => null;

		IControllerTemplateButton IRacingWheelTemplate.wheelButton8 => null;

		IControllerTemplateButton IRacingWheelTemplate.wheelButton9 => null;

		IControllerTemplateButton IRacingWheelTemplate.wheelButton10 => null;

		IControllerTemplateButton IRacingWheelTemplate.consoleButton1 => null;

		IControllerTemplateButton IRacingWheelTemplate.consoleButton2 => null;

		IControllerTemplateButton IRacingWheelTemplate.consoleButton3 => null;

		IControllerTemplateButton IRacingWheelTemplate.consoleButton4 => null;

		IControllerTemplateButton IRacingWheelTemplate.consoleButton5 => null;

		IControllerTemplateButton IRacingWheelTemplate.consoleButton6 => null;

		IControllerTemplateButton IRacingWheelTemplate.consoleButton7 => null;

		IControllerTemplateButton IRacingWheelTemplate.consoleButton8 => null;

		IControllerTemplateButton IRacingWheelTemplate.consoleButton9 => null;

		IControllerTemplateButton IRacingWheelTemplate.consoleButton10 => null;

		IControllerTemplateButton IRacingWheelTemplate.shifter1 => null;

		IControllerTemplateButton IRacingWheelTemplate.shifter2 => null;

		IControllerTemplateButton IRacingWheelTemplate.shifter3 => null;

		IControllerTemplateButton IRacingWheelTemplate.shifter4 => null;

		IControllerTemplateButton IRacingWheelTemplate.shifter5 => null;

		IControllerTemplateButton IRacingWheelTemplate.shifter6 => null;

		IControllerTemplateButton IRacingWheelTemplate.shifter7 => null;

		IControllerTemplateButton IRacingWheelTemplate.shifter8 => null;

		IControllerTemplateButton IRacingWheelTemplate.shifter9 => null;

		IControllerTemplateButton IRacingWheelTemplate.shifter10 => null;

		IControllerTemplateButton IRacingWheelTemplate.reverseGear => null;

		IControllerTemplateButton IRacingWheelTemplate.select => null;

		IControllerTemplateButton IRacingWheelTemplate.start => null;

		IControllerTemplateButton IRacingWheelTemplate.systemButton => null;

		IControllerTemplateButton IRacingWheelTemplate.horn => null;

		IControllerTemplateDPad IRacingWheelTemplate.dPad => null;

		public RacingWheelTemplate(object payload)
			: base(null)
		{
		}
	}
}

#endif
