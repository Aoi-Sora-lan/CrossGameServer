using System.Net;
using System.Text;
using CrossGameServer.Net;

namespace CrossGameServer.Web;

public class DataInjector
{
    public string GenerateHtml(Dictionary<string, string> data)
    {
        var template = File.ReadAllText(Path.Combine("Resources","Html","Index.html"));
        return data.Aggregate(template, (current, kvp) => current.Replace($"{{{{{kvp.Key}}}}}", kvp.Value));
    }
    
    // 生成频道数据HTML
    public string GenerateChannelsData(List<List<MachineEntity>> channels)
    {
        var sb = new StringBuilder();
        
        for (int i = 0; i < channels.Count; i++)
        {
            sb.AppendLine($@"<div class='channel'>
                <div class='channel-header'>
                    <span>频道 {i}</span>
                    <span>{channels[i].Count} 台机器</span>
                </div>
                <div class='machine-list'>");
            
            foreach (var machine in channels[i])
            {
                string statusClass = machine.IOType == MachineIOType.None ? "offline" : "";
                sb.AppendLine($@"<div class='machine {statusClass}'>
                    <div class='machine-info'>
                        <strong>{machine.Name}</strong>
                        <span class='io-type'>{machine.IOType}</span>
                    </div>
                    <div class='machine-address'>
                        {machine.MachineAddress.GameType} | 
                        {ShortenId(machine.MachineAddress.GameId)} | 
                        {ShortenId(machine.MachineAddress.MachineId)}
                    </div>
                </div>");
            }
            
            sb.AppendLine("</div></div>");
        }
        
        return sb.ToString();
    }
    
    // 生成消息数据HTML
    public string GenerateMessagesData(List<MessageLog> messages)
    {
        var sb = new StringBuilder();
        
        foreach (var msg in messages)
        {
            string targetAddr = msg.TargetAddress != null ? 
                $"{msg.TargetAddress?.GameType} | {ShortenId(msg.TargetAddress?.MachineId)}" : 
                "-";
            string sourceAddr = $"{msg.SourceAddress.GameType} | {ShortenId(msg.SourceAddress.MachineId)}";
            string direction = DetermineMessageDirection(msg);
            string directionClass = $"direction-{direction}";
            sb.AppendLine($@"<tr class='message-row'>
                <td><span class='message-direction {directionClass}'>{(direction == "incoming" ? "接收" : "发送")}</span></td>
                <td><span class='message-type type-{msg.MessageType.ToString().ToLower()}'>{msg.MessageType}</span></td>
                <td>{msg.Timestamp:HH:mm:ss}</td>
                <td>{sourceAddr}</td>
                <td>{targetAddr}</td>
                <td>{msg.TargetChannel}</td>
                <td class='message-content'>{msg.Content}</td>
            </tr>");
        }
        
        return sb.ToString();
    }
    private string DetermineMessageDirection(MessageLog message)
    {
        return message.IsFromServer ? "outgoing" : "incoming" ;
    }
    // 生成地址映射数据HTML
    public string GenerateAddressMappingData(Dictionary<MachineAddress, IPEndPoint> mapping)
    {
        var sb = new StringBuilder();
        
        foreach (var kvp in mapping)
        {
            var addr = kvp.Key;
            var endpoint = kvp.Value;
            
            sb.AppendLine($@"<tr>
                <td>{addr.GameType}</td>
                <td>{ShortenId(addr.GameId)}</td>
                <td>{ShortenId(addr.MachineId)}</td>
                <td>{endpoint.Address}</td>
                <td>{endpoint.Port}</td>
            </tr>");
        }
        
        return sb.ToString();
    }
    
    // 辅助方法：缩短长ID显示
    private string ShortenId(string id, int maxLength = 8)
    {
        if (id.Length <= maxLength) return id;
        return id.Substring(0, maxLength/2) + "..." + id.Substring(id.Length - maxLength/2);
    }
}