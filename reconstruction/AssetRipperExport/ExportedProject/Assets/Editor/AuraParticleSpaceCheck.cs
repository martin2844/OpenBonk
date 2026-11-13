using System;
using UnityEditor;
using UnityEngine;

public static class AuraParticleSpaceCheck
{
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/AuraAttack.prefab");
        if (!prefab) throw new Exception("AuraAttack prefab not found");
        var instance = UnityEngine.Object.Instantiate(prefab);
        instance.transform.rotation = Quaternion.Euler(20f, 35f, 15f);
        foreach (var particle in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            var renderer = particle.GetComponent<ParticleSystemRenderer>();
            particle.Simulate(2f, false, true, true);
            var samples = new ParticleSystem.Particle[particle.main.maxParticles];
            int count = particle.GetParticles(samples);
            Debug.Log($"Aura particle space: object={particle.name}, simulation={particle.main.simulationSpace}, scaling={particle.main.scalingMode}, renderer={(renderer ? renderer.renderMode.ToString() : "none")}, alignment={(renderer ? renderer.alignment.ToString() : "none")}, material={(renderer && renderer.sharedMaterial ? renderer.sharedMaterial.name : "none")}, mesh={(renderer && renderer.mesh ? renderer.mesh.name : "none")}, meshBounds={(renderer && renderer.mesh ? renderer.mesh.bounds.size.ToString() : "none")}, live={count}, firstRotation={(count > 0 ? samples[0].rotation3D.ToString() : "none")}, localRotation={particle.transform.localEulerAngles}");
            if (renderer && renderer.sharedMaterial && renderer.sharedMaterial.name == "AuraCircle")
            {
                if (count == 0 || !renderer.mesh || renderer.mesh.bounds.size.z >= renderer.mesh.bounds.size.x * .2f) throw new Exception("Aura ring particle or its thin XY mesh is unavailable");
                Vector3 ringNormal = particle.transform.rotation * Quaternion.Euler(samples[0].rotation3D) * Vector3.forward;
                float angle = Vector3.Angle(ringNormal, instance.transform.up);
                if (angle > .01f) throw new Exception($"Aura ring normal differs from aura up by {angle} degrees");
                Debug.Log($"Aura ring orientation PASS: sampled mesh plane normal differs from tilted aura up by {angle:F3} degrees");
                int checkedParticles = 0;
                foreach (float time in new[] { .25f, .5f, 1f, 2f, 5f, 10f })
                {
                    instance.transform.rotation = Quaternion.Euler(time * 4f, time * 13f, time * -3f);
                    particle.Simulate(time, false, true, true);
                    count = particle.GetParticles(samples);
                    if (count == 0) throw new Exception($"Aura ring has no particles at {time} seconds");
                    for (int i = 0; i < count; i++)
                    {
                        ringNormal = particle.transform.rotation * Quaternion.Euler(samples[i].rotation3D) * Vector3.forward;
                        angle = Vector3.Angle(ringNormal, instance.transform.up);
                        if (angle > .05f) throw new Exception($"Aura ring at {time} seconds differs by {angle} degrees");
                        checkedParticles++;
                    }
                }
                Debug.Log($"Aura animation orientation PASS: {checkedParticles} particle samples across six times and orientations");
            }
        }
        UnityEngine.Object.DestroyImmediate(instance);
    }
}
