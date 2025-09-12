using CrossGameServer.Net;
using CrossGameServer.Web;
using Serilog;

namespace CrossGameServer;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class UdpConsoleApp
{
    static Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();
        Console.Title = "Cross Game Server";
        var defaultPort = 12000;
        var defaultMaxChannel = 8;
        var defaultHttpPort = 8080;
        var defaultMaxHistory = 2047;
        var defaultWebIp = "localhost";
        var config = new Configuration()
        {
            Port = ReadInput(
                prompt: $"请输入服务器端口 (默认: {defaultPort}, 直接回车使用默认值):",
                defaultValue: defaultPort,
                validation: value => value is >= 1024 and <= 65535,
                errorMessage: "端口必须在1024-65535之间"
            ),
            MaxChannel = ReadInput(
                prompt: $"请输入最大频道数 (默认: {defaultMaxChannel}, 直接回车使用默认值):",
                defaultValue: defaultMaxChannel,
                validation: value => value is >= 1 and <= 64,
                errorMessage: "最大频道数必须在1-64之间"
            ),
            HttpPort = ReadInput(
                prompt: $"请输入Web服务器端口 (默认: {defaultHttpPort}, 直接回车使用默认值):",
                defaultValue: defaultHttpPort,
                validation: value => value is >= 1024 and <= 65535,
                errorMessage: "端口必须在1024-65535之间"
            ),
            MaxHistory = ReadInput(
                prompt: $"请输入最大消息记录数 (默认: {defaultMaxHistory}, 直接回车使用默认值):",
                defaultValue: defaultMaxHistory,
                validation: value => value is >= 1 and <= 25565,
                errorMessage: "最大消息记录数必须在1-25565之间"
            ),
            WebIp = ReadString(
                prompt: $"请输入Web服务器Ip (默认: {defaultWebIp}, 直接回车使用默认值):",
                defaultValue: defaultWebIp,
                validation: value => true,
                errorMessage: "")
        };
        Console.WriteLine($"正在启动服务器: 端口={config.Port}, Web端口={config.HttpPort}");
        try
        {
            var udpServer = new BaseUdpServer(config.Port, config.MaxChannel, config.MaxHistory);
            var webServer = new WebServer([$"http://{config.WebIp}:{config.HttpPort}/"], udpServer);
            udpServer.Start();
            webServer.Start();
            Console.WriteLine("服务器启动成功! 按任意键退出...");
            Console.ReadLine();
            webServer.Stop();
            udpServer.Dispose();
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Socket错误: {ex.Message}");
            if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                Console.WriteLine($"端口 {config.Port} 可能已被占用。");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"启动服务器时发生错误: {ex.Message}");
            Console.WriteLine("请检查是否拥有足够的权限");
        }

        return Task.CompletedTask;
    }
    private static int ReadInput(string prompt, int defaultValue, Predicate<int> validation, string errorMessage)
    {
        Console.WriteLine(prompt);
        while (true)
        {
            var input = Console.ReadLine();
            var value = string.IsNullOrWhiteSpace(input) ? defaultValue : ParseInput(input, defaultValue);
        
            if (validation(value))
                return value;
            
            Console.WriteLine($"{errorMessage}，请重新输入 (默认: {defaultValue}):");
        }
    }
    private static string ReadString(string prompt, string defaultValue, Predicate<string> validation, string errorMessage)
    {
        Console.WriteLine(prompt);
        while (true)
        {
            var input = Console.ReadLine();
            var value = string.IsNullOrWhiteSpace(input) ? defaultValue : input;
            if (validation(value))
                return value;
            Console.WriteLine($"{errorMessage}，请重新输入 (默认: {defaultValue}):");
        }
    }
    /// <summary>
    /// 解析用户输入，如果输入无效则返回默认值
    /// </summary>
    private static int ParseInput(string input, int defaultValue)
    {
        if (int.TryParse(input, out int result))
        {
            return result;
        }
        Console.WriteLine($"输入无效，将使用默认值: {defaultValue}");
        return defaultValue;
    }
}