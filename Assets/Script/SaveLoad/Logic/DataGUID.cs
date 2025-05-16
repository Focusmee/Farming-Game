using UnityEngine;
[ExecuteAlways]//让这个脚本需要一直运行
public class DataGUID : MonoBehaviour
{
    public string guid;
    private void Awake()
    {
        if (guid == string.Empty)
        {
            GenerateNewGUID();
        }
    }
    
    /// <summary>
    /// 强制生成一个新的GUID
    /// </summary>
    public void GenerateNewGUID()
    {
        guid = System.Guid.NewGuid().ToString();//生成新的GUID,GUID:是一个十六位的字符串，唯一性的,可以代表着每一个SaveableItem
        Debug.Log($"生成了新的GUID: {guid}");
    }
}
