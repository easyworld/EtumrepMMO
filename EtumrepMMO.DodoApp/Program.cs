using DoDo.Open.Sdk.Models;
using DoDo.Open.Sdk.Services;
using EtumrepMMO.DodoApp;
using Newtonsoft.Json;

Config config;
try
{
    var json = File.ReadAllText("config.json");
    config = JsonConvert.DeserializeObject<Config>(json) ?? new Config();
}
catch (Exception)
{
    Console.WriteLine("Invalid config.json");
    return;
}

if (string.IsNullOrWhiteSpace(config.ClientId) || string.IsNullOrWhiteSpace(config.Token) ||
    string.IsNullOrWhiteSpace(config.ChannelId))
{
    Console.WriteLine("Invalid config.json");
    return;
}

//开放接口服务
var openApiService = new OpenApiService(new OpenApiOptions
{
    BaseApi = "https://botopen.imdodo.com",
    ClientId = config.ClientId,
    Token = config.Token
});
//事件处理服务，可自定义，只要继承EventProcessService抽象类即可
var eventProcessService = new EtumrepSeedService(openApiService, config.ChannelId);
//开放事件服务
var openEventService = new OpenEventService(openApiService, eventProcessService, new OpenEventOptions
{
    IsReconnect = true,
    IsAsync = true
});
//接收事件消息
await openEventService.ReceiveAsync();

Console.ReadKey();