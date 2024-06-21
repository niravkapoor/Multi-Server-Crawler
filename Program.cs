using Crawler.Main;
using Crawler.Node;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

INodeServer NodeServer = new NodeServer();

ICreateServer MainServer = new CreateServer(NodeServer);

MainServer.InitChildServerCreation(2);

app.MapGet("/", () => "Hello World!");

app.MapPost("/v1/url", MainServer.AddUrlForCrawl);

app.MapPost("/v1/server/add", MainServer.AddServerForCrawl);

app.Run("https://localhost:8002");

