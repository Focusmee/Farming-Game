//创建一个接口ISaveable(接口不需要继承Monobehavier)
//接口用interface来创建而不是class
using UnityEngine;

namespace MFarm.Save
{
    public interface ISaveable
    {
        //所有ISaveable派生类都需要实现以下几个函数。

        string GUID { get; }//让每一个继承ISaveable接口的类都需要有一个GUID
        void RegisterSaveable()//创建一个方法，当接口调用的当前接口就会被自动存储到SaveLoadManager中的列表里去
        {
            SaveLoadManager.Instance.RegisterSaveable(this);
        }
        //Save功能函数,返回一个GameSaveData类将各种数据序列化保存到磁盘
        GameSaveData GenerateSaveData();//创建方法，返回值为GameSaveData(该函数是为了存储数据,将各种数据存储出去。所以有一个返回值为GameSaveData。)

        //Load功能函数,将各种序列化数据赋值回来。
        void RestoreData(GameSaveData saveDate);//没有返回值的函数。。。该函数是对过通过获取各种数据来恢复游戏中的变量。
    }
    
    /// <summary>
    /// ISaveable的扩展方法，提供安全的GUID获取功能
    /// </summary>
    public static class ISaveableExtensions
    {
        /// <summary>
        /// 安全地获取或创建DataGUID组件
        /// </summary>
        /// <param name="saveable">实现ISaveable接口的对象</param>
        /// <returns>DataGUID组件，如果获取失败则返回null</returns>
        public static DataGUID GetOrCreateDataGUID(this ISaveable saveable)
        {
            if (saveable is MonoBehaviour monoBehaviour)
            {
                var dataGUID = monoBehaviour.GetComponent<DataGUID>();
                if (dataGUID == null)
                {
                    dataGUID = monoBehaviour.gameObject.AddComponent<DataGUID>();
                    Debug.Log($"为 {monoBehaviour.gameObject.name} 自动添加了 DataGUID 组件");
                }
                return dataGUID;
            }
            return null;
        }
        
        /// <summary>
        /// 安全地获取GUID，如果DataGUID组件不存在会自动创建
        /// </summary>
        /// <param name="saveable">实现ISaveable接口的对象</param>
        /// <returns>GUID字符串，失败时返回空字符串</returns>
        public static string GetSafeGUID(this ISaveable saveable)
        {
            try
            {
                var dataGUID = saveable.GetOrCreateDataGUID();
                return dataGUID?.guid ?? string.Empty;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"获取GUID时发生错误: {ex.Message}");
                return string.Empty;
            }
        }
    }
}


