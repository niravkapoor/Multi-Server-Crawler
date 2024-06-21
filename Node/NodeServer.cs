using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using HtmlAgilityPack;

namespace Crawler.Node
{
    public interface INodeServer
    {
        void CreateWebServer(int port);
    }

	public class NodeServer: INodeServer
    {
        private Queue<string> UrlQueue;
        //private ConcurrentQueue<string> UrlQueue;
        private int NodePort;
        private int RequestCount = 0;
        public NodeServer()
		{
            UrlQueue = new Queue<string>();
        }

        public void CreateWebServer(int port)
        {
            NodePort = port;
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();
            new Thread(
                () =>
                {
                    while (true)
                    {
                        HttpListenerContext ctx = listener.GetContext();
                        ThreadPool.QueueUserWorkItem((_) => ProcessRequest(ctx));
                    }
                }
            ).Start();
        }

        void ProcessRequest(HttpListenerContext ctx)
        {
            string responseText = "Hello";
            byte[] buf = Encoding.UTF8.GetBytes(responseText);


            switch(ctx.Request.Url.AbsolutePath)
            {
                case "/v1/url":
                    if (ctx.Request.HttpMethod == HttpMethod.Post.ToString())
                    {
                        ProcessAddUrlRequest(ctx.Request);
                    }
                    break;
            }
            ctx.Response.ContentEncoding = Encoding.UTF8;
            ctx.Response.ContentType = "text/html";
            ctx.Response.ContentLength64 = buf.Length;


            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
            ctx.Response.Close();
        }

        async void ProcessAddUrlRequest(HttpListenerRequest request)
        {
            try
            {
                using (var reader = new StreamReader(request.InputStream,
                                     request.ContentEncoding))
                {
                    string body = await reader.ReadToEndAsync();
                    body.Split(",").ToList().ForEach(s => UrlQueue.Enqueue(s));
                }

                _ = Task.Run(() => ProcessQueue(request.Url.AbsoluteUri));
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error in node - {request.Url.AbsoluteUri} {ex.Message}");
            }
        }

        private async void ProcessQueue(string absoluteUri)
        {
            List<string> list = new List<string>();
            while (UrlQueue.Count > 0)
            {
                list.Clear();

                string url = UrlQueue.Dequeue();

                RequestCount++;

                Console.WriteLine($"Request Count {RequestCount}");
                var httpClient = new HttpClient();
                try
                {

                    _ = Task.Run(async () =>
                    {
                        var html = await httpClient.GetStringAsync(url);
                        var document = new HtmlDocument();
                        document.LoadHtml(html);
                        var linkNodes = document.DocumentNode.SelectNodes("//a[@href]");
                        if (linkNodes != null)
                        {
                            var links = linkNodes.Where(n => n.Attributes.Contains("href")).Select(n => n.Attributes["href"]).ToList();
                            list = links.Select(l => l.Value).Where((s) => Uri.IsWellFormedUriString(s, UriKind.Absolute)).ToList();
                            if (list.Count > 0)
                            {
                                if (list.Count == 0) throw new ArgumentNullException();

                                _ = Task.Run(() => SendUrlToMaster(list));
                            }
                        }
                    });
                    
                }
                catch(ArgumentNullException err)
                {
                    Console.WriteLine($"ArgumentNullException for {err.Message}");
                }
                catch(HttpRequestException ex)
                {
                    Console.WriteLine($"Exception for {url} {ex.Message}");
                }
            }
        }

        private void SendUrlToMaster(IList<string> list)
        {
            HttpClient client = new HttpClient();
            string urls = string.Join(",", list);
            string _content = JsonConvert.SerializeObject(new { urls = urls });
            //Console.WriteLine($"End time {DateTime.Now.ToString("mm:ss tt")}");

            HttpContent content = new StringContent(_content, Encoding.UTF8, "application/json");
            _ = client.PostAsync($"https://localhost:8000/v1/url", content);
        }
    }
}

