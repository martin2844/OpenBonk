using UnityEngine;

// The desktop Rewired backend is unavailable on Android. InputDevice exposes
// a controller vibrator but does not provide Rewired's separate motor indices.
internal static class AndroidControllerVibration
{
#if UNITY_ANDROID && !UNITY_EDITOR
	private static bool warned;
	private static void ForEachVibrator(System.Action<AndroidJavaObject> action)
	{
		try
		{
			bool hasGamepad = false;
			bool hasControllerVibrator = false;
			using (var input = new AndroidJavaClass("android.view.InputDevice"))
			{
				foreach (int id in input.CallStatic<int[]>("getDeviceIds"))
				using (var device = input.CallStatic<AndroidJavaObject>("getDevice", id))
				{
					if (device == null) continue;
					int sources = device.Call<int>("getSources");
					if ((sources & 0x01000010) != 0x01000010 && (sources & 0x401) != 0x401) continue;
					hasGamepad = true;
					using (var vibrator = device.Call<AndroidJavaObject>("getVibrator"))
						if (vibrator != null && vibrator.Call<bool>("hasVibrator")) { hasControllerVibrator = true; action(vibrator); }
				}
			}
			// Handhelds such as Retroid expose their built-in gamepad and their
			// vibration motor as separate Android devices.
			if (hasGamepad && !hasControllerVibrator)
			using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
			using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
				if (vibrator != null && vibrator.Call<bool>("hasVibrator")) action(vibrator);
		}
		catch (AndroidJavaException e)
		{
			if (!warned) { Debug.LogWarning("Controller vibration unavailable: " + e.Message); warned = true; }
		}
	}

	internal static bool Available()
	{
		bool available = false;
		ForEachVibrator(v => available = true);
		return available;
	}

	internal static void Shake(float intensity, float duration)
	{
		if (duration <= 0f || intensity <= 0f) return;
		long milliseconds = System.Math.Max(1L, (long)(duration * 1000f));
		int amplitude = Mathf.Clamp(Mathf.RoundToInt(intensity * 255f), 1, 255);
		using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
		using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude))
			ForEachVibrator(v => v.Call("vibrate", effect));
	}

	internal static void Stop() { ForEachVibrator(v => v.Call("cancel")); }
#endif
}
