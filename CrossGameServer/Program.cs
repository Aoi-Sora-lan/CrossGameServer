using CrossGameServer.Net;
using Serilog;

namespace CrossGameServer;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class UdpConsoleApp
{
    static Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();
        var udpServer = new BaseUdpServer(11000,8);
        udpServer.Start();
        Console.ReadLine();
        return Task.CompletedTask;
    }
      
}