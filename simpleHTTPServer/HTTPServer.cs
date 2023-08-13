using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace simpleHTTPServer
{
    internal class HTTPServer
    {
        private readonly Dictionary<string, dynamic> config = new()
        {
            { "scheme", "http"},
            { "address", "*"},
            { "port", 80 },
            { "rootPath", "/web" },
            { "log", true }
        };
        public Dictionary<string, Dictionary<string, Action<HttpListenerRequest, Response>>> routeHandlers = new();
        private readonly string cd = Environment.CurrentDirectory;
        private readonly HttpListener listener = new();
        private Logger logger = new("HTTPServer", false);

        /// <summary>
        /// HTTPServer 配置信息
        /// </summary>
        /// <param name="config">配置项字典</param>
        public void Config(Dictionary<string, dynamic> config)
        {
            foreach (KeyValuePair<string, dynamic> kvp in config)
            {
                if (this.config.ContainsKey(kvp.Key))
                {
                    this.config[kvp.Key] = kvp.Value;
                }
            }

            logger = new Logger("HTTPServer", this.config["log"]);
        }

        /// <summary>
        /// 开始 HTTPServer
        /// </summary>
        public void Start()
        {
            config["address"] = config["address"] == "0.0.0.0" ? "*" : config["address"];

            listener.Prefixes.Add($"{config["scheme"]}://{config["address"]}:{config["port"]}/");
            listener.Start();

            logger.Info($"Listening on {config["address"]}:{config["port"]}");

            listener.BeginGetContext(ListenerHandle, listener);
        }

        /// <summary>
        /// 增加路由
        /// </summary>
        /// <param name="route">路由</param>
        /// <param name="method">请求方法</param>
        /// <param name="function">执行函数</param>
        public void AddRoute(string route, string method, Action<HttpListenerRequest, Response> function)
        {
            //routeHandlers[route] = new Dictionary<string, Action<HttpListenerRequest, Response>>();
            //routeHandlers[route][method] = function;

            if (!routeHandlers.ContainsKey(route))
            {
                routeHandlers[route] = new Dictionary<string, Action<HttpListenerRequest, Response>>
                {
                    [method] = function
                };
            }
            else
            {
                routeHandlers[route].Add(method, function);
            }
        }

        private void ListenerHandle(IAsyncResult result)
        {
            if (!listener.IsListening) return;

            listener.BeginGetContext(ListenerHandle, result);

            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            Response response = new(context.Response);

            string rawUrl = request.RawUrl;
            string routeUrl = Regex.Replace(rawUrl, "\\?.*$", "");
            string reqMethod = request.HttpMethod;
            string visitorIP = request.RemoteEndPoint.Address.ToString();

            if (routeHandlers.TryGetValue(routeUrl, out var routeMethod))
            {
                if (routeMethod.TryGetValue(reqMethod, out var routeHandle))
                {
                    logger.Success($"{visitorIP} {reqMethod} 200 Routed success - {rawUrl}");

                    routeHandle(request, response);

                    return;
                }

                logger.Error($"{visitorIP} {reqMethod} 404 Routed not found - {rawUrl}");

                return;
            }

            string rootPath = cd + config["rootPath"];
            string finalDir = rawUrl.Split('/').Last();
            string resourcePath = rootPath + rawUrl + "/index.html";

            if (finalDir != "")
            {
                resourcePath = rootPath + rawUrl;
            }

            if (!File.Exists(resourcePath))
            {
                response.NotFound();
                logger.Error($"{visitorIP} {reqMethod} 404 Not Found - {rawUrl}");

                return;
            }

            logger.Success($"{visitorIP} {reqMethod} 200 Success - {rawUrl}");

            string resourceExt = Path.GetExtension(resourcePath).ToLower();

            switch (resourceExt)
            {
                case ".html":
                    response.SendFile("text/html", resourcePath);
                    break;
                case ".css":
                    response.SendFile("text/css", resourcePath);
                    break;
                case ".js":
                    response.SendFile("application/javascript", resourcePath);
                    break;
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                    response.SendFile("image/" + resourceExt[1..], resourcePath);
                    break;
            }
        }

        private class Logger
        {
            private readonly string title;
            private readonly bool logSwitch;

            public Logger(string title, bool logSwitch)
            {
                this.title = title;
                this.logSwitch = logSwitch;
            }

            private void ConWL(string value)
            {
                Console.WriteLine($"{string.Format("{0:yy-MM-dd HH:mm:ss}", DateTime.Now)} [{this.title}] {value}");
            }

            public void Info(string value)
            {
                if (!logSwitch) return;

                ConWL(value);
            }

            public void Success(string value)
            {
                if (!logSwitch) return;

                Console.ForegroundColor = ConsoleColor.Green;
                ConWL(value);
                Console.ForegroundColor = ConsoleColor.White;
            }

            public void Error(string value)
            {
                if (!logSwitch) return;

                Console.ForegroundColor = ConsoleColor.Red;
                ConWL(value);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        public class Response
        {
            private readonly HttpListenerResponse response;

            public Response(HttpListenerResponse response)
            {
                this.response = response;
            }

            /// <summary>
            /// 响应信息基础框架
            /// </summary>
            /// <param name="contentType">内容格式</param>
            /// <param name="value">内容</param>
            public void Send(string contentType, string value)
            {
                response.StatusCode = 200;
                response.ContentType = contentType + ";charset=UTF-8";
                response.ContentEncoding = Encoding.UTF8;

                using (StreamWriter writer = new(response.OutputStream, Encoding.UTF8))
                {
                    writer.Write(value);
                    writer.Close();
                    response.Close();
                }
            }

            /// <summary>
            /// 响应文件
            /// </summary>
            /// <param name="contentType">内容格式</param>
            /// <param name="filePath">文件路径</param>
            public void SendFile(string contentType, string filePath)
            {
                response.StatusCode = 200;
                response.ContentType = contentType + ";charset=UTF-8";
                response.ContentEncoding = Encoding.UTF8;

                if (contentType.Contains("image/"))
                {
                    using (Stream imageStream = File.OpenRead(filePath))
                    {
                        imageStream.CopyTo(response.OutputStream);
                        response.Close();
                    }

                    return;
                }

                using (StreamWriter writer = new StreamWriter(response.OutputStream, Encoding.UTF8))
                {
                    writer.Write(File.ReadAllText(filePath));
                    writer.Close();
                    response.Close();
                }
            }

            /// <summary>
            /// 响应文本
            /// </summary>
            /// <param name="value">文本内容</param>
            public void SendText(string value)
            {
                Send("text/plain", value);
            }

            /// <summary>
            /// 响应 Json 数据
            /// </summary>
            /// <param name="jsonData">Json 数据对象</param>
            public void SendJson(object jsonData)
            {
                string json = JsonSerializer.Serialize(jsonData);
                json = Regex.Unescape(json);
                Send("application/json", json);
            }

            /// <summary>
            /// 错误状态响应 404
            /// </summary>
            public void NotFound()
            {
                response.StatusCode = 404;
                response.ContentType = "text/html;charset=UTF-8";
                response.ContentEncoding = Encoding.UTF8;

                using (StreamWriter writer = new StreamWriter(response.OutputStream, Encoding.UTF8))
                {
                    writer.Write("""
                        <!DOCTYPE html>
                        <html>
                        <head>
                            <meta charset="UTF-8">
                            <meta name="viewport" content="width=device-width, initial-scale=1.0">
                            <title>404 NOT FOUND</title>
                        </head>
                        <body>
                            <h1 style="text-align: center;margin-top: 40px;">404 NOT FOUND</h1>
                            <h2 style="text-align: center;margin-top: 20px;">啥也没找到！</h2>
                        </body>
                        </html>
                        """);
                    writer.Close();
                    response.Close();
                }
            }

            /// <summary>
            /// 成功响应 204 无内容回复
            /// </summary>
            /// <param name="msg"></param>
            public void NotContent(string msg)
            {
                response.StatusCode = 204;
                response.ContentType = "application/json;charset=UTF-8";
                response.ContentEncoding = Encoding.UTF8;

                using StreamWriter writer = new(response.OutputStream, Encoding.UTF8);
                writer.Write(msg);
                writer.Close();
                response.Close();
            }
        }
    }
}
