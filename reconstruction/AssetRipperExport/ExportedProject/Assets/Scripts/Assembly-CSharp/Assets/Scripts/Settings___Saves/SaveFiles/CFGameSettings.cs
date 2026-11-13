using System;
using System.Collections.Generic;

namespace Assets.Scripts.Settings___Saves.SaveFiles
{
	[Serializable]
	public class CFGameSettings : CFSettings
	{
		public int camera_shake = 1;

		public float input_delay = 2f;

		public int enemy_targeting_mode;

		public float random_enemy_targeting = 0.75f;

		public int language;

		public int show_tips = 1;

		public float minimap_size = 1.35f;

		public int minimap_rotation = 1;

		public int hp_bar_color;

		public float crosshair_height = 0.5f;

		public float crosshair_size = 0.9f;

		public float crosshair_alpha = 1f;

		public int hide_leaderboards;

		public int upload_score_to_leaderboard = 1;

		public int quick_reset = 1;

		public int debug_shrines;

		public int debug_fps;

		public int debug_speed;

		public int debug_ram;

		public int debug_enemy_scaling;

		public int pege_mode;

		public int unlock_all;

		public int auto_select_upgrades;

		public float auto_select_after_level = 200f;

		public int show_advanced_settings;

		public int show_hud = 1;

		public int skip_chest_animation;

		public int show_item_feed;

		public int skip_portal_animation;

		public int enable_silver_pots = 1;

		public float quick_reset_time = 1f;

		public int super_quick_resets;

		public List<SettingHeader> GetHeaders()
		{
			return new List<SettingHeader> { new SettingHeader(0, "GAME"), new SettingHeader(4, "UI"), new SettingHeader(9, "OTHER"), new SettingHeader(25, "ADVANCED") };
		}
	}
}
