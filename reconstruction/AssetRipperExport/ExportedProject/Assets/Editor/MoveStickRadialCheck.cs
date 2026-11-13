using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class MoveStickRadialCheck
{
    public static void Run()
    {
        var flags = BindingFlags.Static | BindingFlags.NonPublic;
        var radial = typeof(AndroidNativeInput).GetMethod("ApplyMoveStickDeadZone", flags);
        var combine = typeof(AndroidNativeInput).GetMethod("CombineMoveAxis", flags);
        if (radial == null || combine == null) throw new Exception("Move stick methods unavailable");
        var raw = new[] { Vector2.zero, new Vector2(0f,6553f/32767f), new Vector2(0f,9830f/32767f), new Vector2(6553f/32767f,6553f/32767f), new Vector2(9830f/32767f,3277f/32767f), new Vector2(-13107f/32767f,6553f/32767f), new Vector2(23170f/32767f,23170f/32767f), Vector2.one };
        var expected = new[] { Vector2.zero, Vector2.zero, new Vector2(0f,.066663f), new Vector2(.030948f,.030948f), new Vector2(.083771f,.027927f), new Vector2(-.235195f,.117588f), new Vector2(.707116f,.707116f), Vector2.one };
        for (int i=0;i<raw.Length;i++)
        {
            var actual=(Vector2)radial.Invoke(null,new object[] { raw[i] });
            if (Vector2.Distance(actual,expected[i])>.00001f) throw new Exception($"PC sample {i}: expected {expected[i]}, got {actual}");
        }
        float Merge(float keyboard, float stick) => (float)combine.Invoke(null,new object[] { keyboard,stick });
        if (Merge(1f,.3f)!=1f || Merge(0f,-.3f)!=-.3f || Merge(-.6f,.2f)!=-.6f || Merge(.1f,-.7f)!=-.7f) throw new Exception("Keyboard/controller arbitration differs");
        var axes=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]).FindProperty("m_Axes");
        int keyboardX=0,keyboardY=0,stickX=0,stickY=0;
        for (int i=0;i<axes.arraySize;i++)
        {
            var axis=axes.GetArrayElementAtIndex(i); string name=axis.FindPropertyRelative("m_Name").stringValue;
            if (name=="NativeMoveHorizontal" || name=="NativeMoveVertical")
            {
                if (axis.FindPropertyRelative("type").enumValueIndex!=0) throw new Exception("Movement keyboard axis also contains joystick");
                if (name=="NativeMoveHorizontal") keyboardX++; else keyboardY++;
            }
            if (name=="NativeStickMoveHorizontal" || name=="NativeStickMoveVertical")
            {
                bool x=name=="NativeStickMoveHorizontal";
                if (axis.FindPropertyRelative("type").enumValueIndex!=2 || axis.FindPropertyRelative("axis").enumValueIndex!=(x ? 0 : 1) || axis.FindPropertyRelative("dead").floatValue!=0f || axis.FindPropertyRelative("invert").boolValue==x) throw new Exception("Unexpected raw stick axis configuration: "+name);
                if (x) stickX++; else stickY++;
            }
        }
        if (keyboardX!=1 || keyboardY!=1 || stickX!=1 || stickY!=1) throw new Exception($"Unexpected axes: keyboard {keyboardX},{keyboardY}; stick {stickX},{stickY}");
        Debug.Log("MoveStickRadialCheck PASS: observed PC samples, keyboard arbitration and raw axis configuration");
    }
}
