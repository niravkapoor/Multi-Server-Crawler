using System.Text;
using Newtonsoft.Json;
using Crawler.Main.Models;
using System.Collections.Concurrent;

namespace Crawler.Main
{
	interface ICreateServer
	{
		public void InitChildServerCreation(int serverCount);
		public void AddUrlForCrawl(HttpRequest req, HttpResponse res);
		public void AddServerForCrawl(HttpRequest req, HttpResponse res);
    }

	public class CreateServer: ICreateServer
    {
		private readonly Node.INodeServer NodeServer;
		private Queue<string> UrlQueue;
        IDictionary<int, string> NodeServerMap;
		private int NextAvailablePort;
		BlockingCollection<string> Blockingcollect = new BlockingCollection<string>();
		int UrlCount = 0;

		public CreateServer(Node.INodeServer nodeServer)
		{
			this.NodeServer = nodeServer;
			NextAvailablePort = 3000;
            UrlQueue = new Queue<string>();
			NodeServerMap = new Dictionary<int, string>();
        }

		public void InitChildServerCreation(int serverCount)
		{
			for(int i = 0; i < serverCount; i++)
			{
				AddServer(NextAvailablePort++);
            }
			ProcessQueue();
		}

		public async void AddUrlForCrawl(HttpRequest req, HttpResponse res)
		{
			//Console.WriteLine($"Start time {DateTime.Now.ToString("mm:ss tt")}");
			Url result;
            using (StreamReader stream = new StreamReader(req.Body, encoding: Encoding.UTF8))
            {
				string body = await stream.ReadToEndAsync();

				result = JsonConvert.DeserializeObject<Url>(body);

				result.UrlList.ForEach(s => UrlQueue.Enqueue(s));
            }

			await res.CompleteAsync();
        }

		public async void AddServerForCrawl(HttpRequest req, HttpResponse res)
		{
			AddServer result;
            using (StreamReader stream = new StreamReader(req.Body, encoding: Encoding.UTF8))
            {
                string body = await stream.ReadToEndAsync();

                result = JsonConvert.DeserializeObject<AddServer>(body);

				if (result != null)
					_ = Task.Run(() => InitChildServerCreation(result.count));

				await res.CompleteAsync();
            }
        }

		private void ProcessQueue()
		{
			int serverCount = NodeServerMap.Count;
            Random rnd = new Random();
			IList<string> serverList = NodeServerMap.Values.ToList();

			IDictionary<string, List<string>> requestToServer = new Dictionary<string, List<string>>();

			while (UrlQueue.Count > 0)
			{
				string url = UrlQueue.Dequeue();

				int random = rnd.Next(0, serverCount);
				if (!requestToServer.ContainsKey(serverList[random]))
				{
					requestToServer.Add(serverList[random], new List<string>());
				}

				requestToServer[serverList[random]].Add(url);
			}


			foreach (KeyValuePair<string, List<string>> item in requestToServer){
                Task.Run(() => SendRequestToNode(item.Key, string.Join(",", item.Value)));
            }
		}

		private async void SendRequestToNode(string node, string url)
		{
			HttpClient client = new HttpClient();
			
			HttpContent content = new StringContent(url, Encoding.UTF8, "application/json");

			_ = client.PostAsync($"{node}/v1/url", content);
        }

		private void AddServer(int port)
		{
            NodeServer.CreateWebServer(port);
            NodeServerMap.Add(port, $"http://localhost:{port}");
        }
    }
}

