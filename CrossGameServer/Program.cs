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
        Console.WriteLine($"请输入服务器端口 (默认: {defaultPort}, 直接回车使用默认值):");
        var portInput = Console.ReadLine();
        int port = string.IsNullOrWhiteSpace(portInput) ? defaultPort : ParseInput(portInput, defaultPort);
        while (port is < 1024 or > 65535)
        {
            Console.WriteLine($"端口必须在1024-65535之间，请重新输入 (默认: {defaultPort}):");
            portInput = Console.ReadLine();
            port = string.IsNullOrWhiteSpace(portInput) ? defaultPort : ParseInput(portInput, defaultPort);
        }
        Console.WriteLine($"请输入最大频道数 (默认: {defaultMaxChannel}, 直接回车使用默认值):");
        var maxChannelInput = Console.ReadLine();
        int maxChannel = string.IsNullOrWhiteSpace(maxChannelInput) ? defaultMaxChannel : ParseInput(maxChannelInput, defaultMaxChannel);
        
        while (maxChannel is < 1 or > 64)
        {
            Console.WriteLine($"最大频道数必须在1-64之间，请重新输入 (默认: {defaultMaxChannel}):");
            maxChannelInput = Console.ReadLine();
            maxChannel = string.IsNullOrWhiteSpace(maxChannelInput) ? defaultMaxChannel : ParseInput(maxChannelInput, defaultMaxChannel);
        }
        Console.WriteLine($"正在启动服务器: 端口={port}, 最大频道数={maxChannel}");
        try
        {
            var udpServer = new BaseUdpServer(port, maxChannel);
            var webServer = new WebServer(["http://localhost:8080/"], udpServer);
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
                Console.WriteLine($"端口 {port} 可能已被占用。");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"启动服务器时发生错误: {ex.Message}");
        }

        return Task.CompletedTask;
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