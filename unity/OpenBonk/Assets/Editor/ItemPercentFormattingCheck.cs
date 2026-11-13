using System;
using System.Globalization;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
public static class ItemPercentFormattingCheck
{
    sealed class Provider: ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T:LocalizationTable
        {
            var table=AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/"+name+"_"+locale.Identifier.Code+".asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T,null);
        }
    }
    public static void Run()
    {
        LocalizationSettings.Instance=ScriptableObject.CreateInstance<LocalizationSettings>();
        LocalizationSettings.StringDatabase.TableProvider=new Provider();
        string[] names={"Beefy Ring","Boss Buster","Moldy Cheese","Tactical Glasses","Turbo Socks"};
        string[] values={"+20%","+15%","+40%","+20%","+15%"};
        int failures=0,checkedCount=0;
        foreach(string code in new[]{"en","es"})
        {
            CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(code=="en"?"en-US":"es-ES");
            LocalizationSettings.SelectedLocale=Locale.CreateLocale(code);
            for(int i=0;i<names.Length;i++)
            {
                var item=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/"+names[i]+".asset");
                string text=item!=null?item.GetDescription():"<missing item>";
                bool pass=text.Contains(values[i]);
                if(!pass)failures++;
                checkedCount++;
                Debug.Log($"ItemPercentFormattingCheck {(pass?"PASS":"FAIL")} locale={code} item={names[i]} expected={values[i]} text={text}");
            }
        }
        Debug.Log($"ItemPercentFormattingCheck result checked={checkedCount} failures={failures}");
        if(failures!=0)throw new Exception("Item percentage formatting mismatch");
    }
}
