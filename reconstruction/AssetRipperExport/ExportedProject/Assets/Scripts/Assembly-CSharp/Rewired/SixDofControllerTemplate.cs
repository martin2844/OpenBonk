#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;

namespace Rewired
{
	public sealed class SixDofControllerTemplate : ControllerTemplate, ISixDofControllerTemplate, IControllerTemplate
	{
		public static readonly Guid typeGuid;

		public const int elementId_positionX = 1;

		public const int elementId_positionY = 2;

		public const int elementId_positionZ = 0;

		public const int elementId_rotationX = 3;

		public const int elementId_rotationY = 5;

		public const int elementId_rotationZ = 4;

		public const int elementId_throttle1Axis = 6;

		public const int elementId_throttle1MinDetent = 50;

		public const int elementId_throttle2Axis = 7;

		public const int elementId_throttle2MinDetent = 51;

		public const int elementId_extraAxis1 = 8;

		public const int elementId_extraAxis2 = 9;

		public const int elementId_extraAxis3 = 10;

		public const int elementId_extraAxis4 = 11;

		public const int elementId_button1 = 12;

		public const int elementId_button2 = 13;

		public const int elementId_button3 = 14;

		public const int elementId_button4 = 15;

		public const int elementId_button5 = 16;

		public const int elementId_button6 = 17;

		public const int elementId_button7 = 18;

		public const int elementId_button8 = 19;

		public const int elementId_button9 = 20;

		public const int elementId_button10 = 21;

		public const int elementId_button11 = 22;

		public const int elementId_button12 = 23;

		public const int elementId_button13 = 24;

		public const int elementId_button14 = 25;

		public const int elementId_button15 = 26;

		public const int elementId_button16 = 27;

		public const int elementId_button17 = 28;

		public const int elementId_button18 = 29;

		public const int elementId_button19 = 30;

		public const int elementId_button20 = 31;

		public const int elementId_button21 = 55;

		public const int elementId_button22 = 56;

		public const int elementId_button23 = 57;

		public const int elementId_button24 = 58;

		public const int elementId_button25 = 59;

		public const int elementId_button26 = 60;

		public const int elementId_button27 = 61;

		public const int elementId_button28 = 62;

		public const int elementId_button29 = 63;

		public const int elementId_button30 = 64;

		public const int elementId_button31 = 65;

		public const int elementId_button32 = 66;

		public const int elementId_hat1Up = 32;

		public const int elementId_hat1UpRight = 33;

		public const int elementId_hat1Right = 34;

		public const int elementId_hat1DownRight = 35;

		public const int elementId_hat1Down = 36;

		public const int elementId_hat1DownLeft = 37;

		public const int elementId_hat1Left = 38;

		public const int elementId_hat1UpLeft = 39;

		public const int elementId_hat2Up = 40;

		public const int elementId_hat2UpRight = 41;

		public const int elementId_hat2Right = 42;

		public const int elementId_hat2DownRight = 43;

		public const int elementId_hat2Down = 44;

		public const int elementId_hat2DownLeft = 45;

		public const int elementId_hat2Left = 46;

		public const int elementId_hat2UpLeft = 47;

		public const int elementId_hat1 = 48;

		public const int elementId_hat2 = 49;

		public const int elementId_throttle1 = 52;

		public const int elementId_throttle2 = 53;

		public const int elementId_stick = 54;

		IControllerTemplateAxis ISixDofControllerTemplate.extraAxis1 => null;

		IControllerTemplateAxis ISixDofControllerTemplate.extraAxis2 => null;

		IControllerTemplateAxis ISixDofControllerTemplate.extraAxis3 => null;

		IControllerTemplateAxis ISixDofControllerTemplate.extraAxis4 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button1 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button2 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button3 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button4 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button5 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button6 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button7 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button8 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button9 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button10 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button11 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button12 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button13 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button14 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button15 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button16 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button17 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button18 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button19 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button20 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button21 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button22 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button23 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button24 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button25 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button26 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button27 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button28 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button29 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button30 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button31 => null;

		IControllerTemplateButton ISixDofControllerTemplate.button32 => null;

		IControllerTemplateHat ISixDofControllerTemplate.hat1 => null;

		IControllerTemplateHat ISixDofControllerTemplate.hat2 => null;

		IControllerTemplateThrottle ISixDofControllerTemplate.throttle1 => null;

		IControllerTemplateThrottle ISixDofControllerTemplate.throttle2 => null;

		IControllerTemplateStick6D ISixDofControllerTemplate.stick => null;

		public SixDofControllerTemplate(object payload)
			: base(null)
		{
		}
	}
}

#endif
