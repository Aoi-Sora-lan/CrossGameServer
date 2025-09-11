using System.Net;
using System.Text;
using CrossGameServer.Net;

namespace CrossGameServer.Web;

class WebServer
{
    private readonly HttpListener _listener = new HttpListener();
    private readonly BaseUdpServer _udpServer;
    private readonly Func<HttpListenerRequest, string> _responseHandler;
    public string HandleRequest(HttpListenerRequest request)
    {
        // 根据请求路径返回不同内容
        return request.Url.AbsolutePath switch
        {
            "/" => HomePage(),
            "/about" => AboutPage(),
            _ => ErrorPage()
        };
    }

    string HomePage()
    {
        var injector = new DataInjector();
        var channels = _udpServer.GetChannels();
        var machines = _udpServer.GetMachineMapper();
        var messages = _udpServer.GetMessages();
        var data = new Dictionary<string, string>
        {
            {"SERVER_TIME", DateTime.Now.ToString()},
            {"ACTIVE_CHANNELS_COUNT", channels.Count.ToString()},
            {"CONNECTED_MACHINES_COUNT", machines.Count.ToString()},
            {"TODAY_MESSAGES_COUNT", messages.Count.ToString()},
            {"CHANNELS_DATA", injector.GenerateChannelsData(channels)},
            {"MESSAGES_DATA", injector.GenerateMessagesData(messages)},
            {"ADDRESS_MAPPING_DATA", injector.GenerateAddressMappingData(machines)}
        };

        string finalHtml = injector.GenerateHtml(data);
        return finalHtml;
    }

    static string AboutPage() => @"
        <!DOCTYPE html>
        <html>
        <head><title>About</title></head>
        <body>
            <nav>
                <a href='/'>Home</a> | 
                <a href='/about'>About</a>
            </nav>
            <h1>About This Server</h1>
            <p>This is a simple HTTP server built with C#</p>
        </body>
        </html>";

    static string ErrorPage() => @"
        <!DOCTYPE html>
        <html>
        <head><title>Error</title></head>
        <body>
            <h1>404 - Page Not Found</h1>
            <p>Try visiting <a href='/'>home page</a></p>
        </body>
        </html>";

    public WebServer(string[] prefixes, BaseUdpServer udpServer)
    {
        if (!HttpListener.IsSupported)
            throw new NotSupportedException("HttpListener not supported");
        _udpServer = udpServer;

        foreach (string prefix in prefixes)
            _listener.Prefixes.Add(prefix);

        _responseHandler = HandleRequest;
    }

    public void Start()
    {
        _listener.Start();
        ThreadPool.QueueUserWorkItem(o =>
        {
            try
            {
                while (_listener.IsListening)
                {
                    ThreadPool.QueueUserWorkItem(c =>
                    {
                        var ctx = c as HttpListenerContext;
                        try
                        {
                            string response = _responseHandler(ctx.Request);
                            byte[] buf = Encoding.UTF8.GetBytes(response);
                            ctx.Response.ContentType = "text/html";
                            ctx.Response.ContentLength64 = buf.Length;
                            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                        }
                        catch { }
                        finally
                        {
                            ctx.Response.Close();
                        }
                    }, _listener.GetContext());
                }
            }
            catch { }
        });
    }

    public void Stop() => _listener.Stop();
}

