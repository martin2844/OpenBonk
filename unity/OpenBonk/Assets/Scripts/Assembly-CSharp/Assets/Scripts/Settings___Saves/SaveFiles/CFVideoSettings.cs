using System.Collections.Generic;

namespace Assets.Scripts.Settings___Saves.SaveFiles
{
	public class CFVideoSettings : CFSettings
	{
		public int resolution = -1;

		public int target_monitor;

		public int fullscreen_mode = 1;

		public int vsync = 1;

		public float fps_limit = 144f;

		public float fov = 85f;

		public float camera_distance;

		public int grass_quality = 1;

		public int shadow_quality = 1;

		public int shadow_resolution;

		public int anti_aliasing;

		public int soft_particles = 1;

		public int bloom = 1;

		public int motion_blur;

		public int ambient_occlusion = 1;

		public List<SettingHeader> GetHeaders()
		{
			return new List<SettingHeader> { new SettingHeader(0, "SCREEN"), new SettingHeader(7, "GRAPHICS") };
		}
	}
}
