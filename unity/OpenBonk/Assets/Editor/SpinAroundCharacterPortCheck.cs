using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class SpinAroundCharacterPortCheck
{
    public static void Run()
    {
        var root=new GameObject("orbit fixture"); var target=new GameObject("orbit target"); var camera=new GameObject("orbit camera");
        try
        {
            target.transform.position=new Vector3(3,4,5); camera.transform.SetParent(root.transform);
            var orbit=root.AddComponent<SpinAroundCharacter>(); orbit.target=target.transform; orbit.camera=camera.transform;
            if (orbit.distanceFromTarget!=10f || orbit.cameraHeight!=5f || orbit.rotationSpeed!=.5f)
                throw new Exception("Orbit constructor defaults differ from original");
            orbit.cameraHeight=2; orbit.distanceFromTarget=7; orbit.targetOffset=new Vector3(0,1,0); orbit.rotationSpeed=30;
            var update=typeof(SpinAroundCharacter).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic);
            update.Invoke(orbit,null);
            if(Vector3.Distance(root.transform.position,target.transform.position)>.0001f || Vector3.Distance(camera.transform.localPosition,new Vector3(0,2,-7))>.0001f)
                throw new Exception("Orbit position did not follow target and offset");
            var expected=(target.transform.position+orbit.targetOffset-camera.transform.position).normalized;
            if(Vector3.Dot(camera.transform.forward,expected)<.9999f) throw new Exception("Camera did not face target offset");
            var field=typeof(SpinAroundCharacter).GetField("currentAngle",BindingFlags.Instance|BindingFlags.NonPublic);
            float angle=(float)field.GetValue(orbit);
            if(Mathf.Abs(Mathf.DeltaAngle(root.transform.eulerAngles.y,angle))>.001f) throw new Exception("Orbit rotation did not follow angle");
            orbit.target=null; update.Invoke(orbit,null); orbit.target=target.transform; orbit.camera=null; update.Invoke(orbit,null);
            Debug.Log("SpinAroundCharacterPortCheck PASS: follow, local offset, look direction, rotation and null guards");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(target); }
    }
}
