using System.Collections.Generic;

namespace Assets.Scripts.Settings___Saves.SaveFiles
{
	public class CFControlSettings : CFSettings
	{
		public float sensitivity = 1f;

		public float look_smoothing = 0.6f;

		public float controller_sensitivity = 3.2f;

		public int inverted_horizontal_axis;

		public int inverted_vertical_axis;

		public int hold_crouch = 1;

		public int hold_aim;

		public int rotate_camera_with_arrow_keys;

		public int select_upgrades_with_number_keys = 1;

		public string keyboard_move_forward = "Move Vertical";

		public string keyboard_move_backward = "Move Vertical";

		public string keyboard_move_left = "Move Horizontal";

		public string keyboard_move_right = "Move Horizontal";

		public string keyboard_jump = "Jump";

		public string keyboard_interact = "Interact";

		public string keyboard_slide = "Slide";

		public string keyboard_aim = "Aim";

		public string keyboard_quick_reset = "QuickReset";

		public string map_overlay = "MapOverlay";

		public int controller;

		public float controller_vibration = 1f;

		public string controller_jump = "Jump";

		public string controller_interact = "Interact";

		public string controller_slide = "Slide";

		public string controller_aim = "Aim";

		public string controller_quick_reset = "QuickReset";

		public string controller_map_overlay = "MapOverlay";

		public int swap_menu_ab;

		public int pause_on_controller_disconnect = 1;

		public int space_navigation = 1;

		public int horizontal_navigation;

		public string keyboard_jump_ignore_extra_jumps = "Jump Bhop";

		public string keyboard_hold_to_wallrun = "Wallrun";

		public string controller_hold_to_wallrun = "Wallrun";

		// Android on-screen controls are opt-in; old saves default to controller input.
		public int touch_controls;

		public List<SettingHeader> GetHeaders()
		{
			return new List<SettingHeader> { new SettingHeader(0, "GENERAL"), new SettingHeader(9, "KEYBOARD INPUT"), new SettingHeader(19, "CONTROLLER INPUT"), new SettingHeader(28, "EXTRA"), new SettingHeader(31, "ADVANCED") };
		}
	}
}
