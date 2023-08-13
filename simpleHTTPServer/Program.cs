namespace simpleHTTPServer
{
    class Program
    {
        static void Main(string[] args)
        {
            HTTPServer hs = new();

            hs.Config(new Dictionary<string, dynamic> {
                { "address", "127.0.0.1" },
                { "port", 80 }
            });

            hs.Start();

            hs.AddRoute("/", "GET", (req, res) =>
            {
                res.SendText("Hello, world!");
            });

            Console.ReadLine();
        }
    }
}