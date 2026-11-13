using System.Numerics;
using UnityEngine;

public class VectorExtensions : MonoBehaviour
{
	public static UnityEngine.Vector3 XZVector(UnityEngine.Vector3 v)
	{
		return new UnityEngine.Vector3(v.x, 0f, v.z);
	}

	public static UnityEngine.Vector2 XZVector2(UnityEngine.Vector3 v)
	{
		return new UnityEngine.Vector2(v.x, v.z);
	}

	public static System.Numerics.Vector3 UnityToNet(UnityEngine.Vector3 vec)
	{
		return new System.Numerics.Vector3(vec.x, vec.y, vec.z);
	}

	public static UnityEngine.Vector3 NetToUnity(System.Numerics.Vector3 vec)
	{
		return new UnityEngine.Vector3(vec.X, vec.Y, vec.Z);
	}

	public static UnityEngine.Vector3 ClampVector(UnityEngine.Vector3 vec, float min, float max)
	{
		return new UnityEngine.Vector3(Mathf.Clamp(vec.x, min, max), Mathf.Clamp(vec.y, min, max), Mathf.Clamp(vec.z, min, max));
	}
}
