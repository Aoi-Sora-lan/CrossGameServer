using System.Reflection;
using cfg;
using cfg.main;
using CrossGameServer.Requests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CrossGameServer;

public class ItemTransferHelper
{
    private Tables _tables = new Tables(LoadJson);
    private static JArray? LoadJson(string file)
    {
        return JsonConvert.DeserializeObject(
            File.ReadAllText(Path.Combine("Resources","Data",$"{file}.json"))) as JArray;
    }
    public static ItemTransferHelper Instance
    {
        get
        {
            _instance ??= new ItemTransferHelper();
            return _instance;
        }
    }
    private static ItemTransferHelper? _instance = null;
    
    private string? GetTypeId(string gameType, string gameId)
    {
        // 获取ItemMapperBean类型
        var bean = _tables.Item[gameId];
        var type = typeof(ItemMapperBean);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        Log.Debug($"gameType: {gameType}");
        var idField = fields.First(f => f.Name == $"{gameType}Id");
        var result = (string)idField.GetValue(bean)!;
        Log.Debug(result);
        return result;
    }

    private int GetUniCount(string gameType, string gameId)
    {
        // 获取ItemMapperBean类型
        var bean = _tables.Item[gameId];
        var type = typeof(ItemMapperBean);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        var countField = fields.First(f => f.Name == $"{gameType}Count");
        var result = (int)countField.GetValue(bean)!;
        Log.Debug(result.ToString());
        return result;
    }
    public TransferResult Transfer(string itemId, int itemCount, string inputGameType, string outputGameType)
    {
        var uniId = GetUniId(inputGameType, itemId);
        var transferPackage = new ItemPackage();
        var uniCount = uniId == null? -1 : GetUniCount(inputGameType, uniId);
        var groupCount = itemCount / uniCount;
        if (uniId != null&&uniCount != -1)
        {
            var transferId = GetTypeId(outputGameType, uniId)!;
            var transferCount = GetUniCount(outputGameType, uniId);
            transferPackage.ItemId = transferId;
            transferPackage.ItemCount = groupCount * transferCount;
        }
        var otherCount = uniCount == -1 ? itemCount : itemCount % uniCount;
        return new TransferResult()
        {
            TransferPackage = transferPackage,
            OtherPackage = new ItemPackage()
            {
                ItemId = inputGameType,
                ItemCount = otherCount
            }
        };
    }
    

    private string? GetUniId(string gameType, string typeId)
    {
        var type = typeof(ItemMapperBean);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        var idField = fields.First(f => f.Name == $"{gameType}Id");
        foreach (var itemMapperBean in _tables.Item.DataList)
        {
            var str = (string)idField.GetValue(itemMapperBean!)!;
            if (str == typeId) return itemMapperBean.UniId;
        }

        return null;
    }
    
}

public struct TransferResult
{
    public ItemPackage TransferPackage;
    public ItemPackage OtherPackage;
}
