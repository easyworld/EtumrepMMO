using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DoDo.Open.Sdk.Models.Bots;
using DoDo.Open.Sdk.Models.Channels;
using DoDo.Open.Sdk.Models.Events;
using DoDo.Open.Sdk.Models.Messages;
using DoDo.Open.Sdk.Services;
using EtumrepMMO.Lib;
using Newtonsoft.Json;
using PKHeX.Core;

namespace EtumrepMMO.DodoApp
{
    public class EtumrepSeedService : EventProcessService
    {
        private readonly OpenApiService _openApiService;
        private readonly string _channelId;
        private readonly string _botDodoId;
        private object sync = new object();

        public EtumrepSeedService(OpenApiService openApiService, string channelId)
        {
            _openApiService = openApiService;
            var output = _openApiService.GetBotInfo(new GetBotInfoInput());
            _botDodoId = output != null ? output.DodoId : "0";
            _channelId = channelId;
        }

        public override void Connected(string message)
        {
            Console.WriteLine($"{message}\n");
        }

        public override void Disconnected(string message)
        {
            Console.WriteLine($"{message}\n");
        }

        public override void Reconnected(string message)
        {
            Console.WriteLine($"{message}\n");
        }

        public override void Exception(string message)
        {
            Console.WriteLine($"{message}\n");
        }

        public override void ChannelMessageEvent<T>(
            EventSubjectOutput<EventSubjectDataBusiness<EventBodyChannelMessage<T>>> input)
        {
            var eventBody = input.Data.EventBody;

            if (eventBody.ChannelId != _channelId) return;

            if (eventBody.MessageBody is MessageBodyText messageBodyText)
            {
                var messageBody = messageBodyText;

                var content = messageBody.Content;

                if (!content.Contains($"<@!{_botDodoId}>")) return;
                content = content.Replace($"<@!{_botDodoId}>", "");
                if (!content.Trim().StartsWith("seed")) return;

                var inputs = new List<PKM>();
                try
                {
                    var lines = Regex.Split(content.Trim(), "\\s").Where(str => !string.IsNullOrWhiteSpace(str))
                        .ToList();
                    PA8 pa8 = new PA8();
                    foreach (var line in lines)
                    {
                        var splitArray = line.Trim().Split(":");
                        if (splitArray[0] == "species" && pa8.Species != 0)
                        {
                            inputs.Add(pa8);
                            pa8 = new PA8();
                        }

                        switch (splitArray[0])
                        {
                            case "species":
                                pa8.Species = int.Parse(splitArray[1]);
                                break;
                            case "pid":
                                pa8.PID = uint.Parse(splitArray[1]);
                                break;
                            case "ec":
                                pa8.EncryptionConstant = uint.Parse(splitArray[1]);
                                break;
                            case "IVs":
                                pa8.IVs = splitArray[1].Split(",").Select(int.Parse).ToArray();
                                break;
                            case "TID":
                                pa8.TID = int.Parse(splitArray[1]);
                                break;
                            case "SID":
                                pa8.SID = int.Parse(splitArray[1]);
                                break;
                        }
                    }

                    if (pa8.Species != 0) inputs.Add(pa8);
                }
                catch (Exception)
                {
                    SendChannelAtMessage(eventBody.DodoId, "非法格式，请检查", _channelId);
                    return;
                }

                if (inputs.Count < 2)
                {
                    SendChannelAtMessage(eventBody.DodoId, "有效宝可梦数量小于2个，请检查输入", _channelId);
                    return;
                }
                else if (inputs.Count > 4)
                {
                    SendChannelAtMessage(eventBody.DodoId, "有效宝可梦数量大于4个，请检查输入", _channelId);
                    return;
                }

                if (Monitor.TryEnter(sync))
                {
                    try
                    {
                        SendChannelAtMessage(eventBody.DodoId, $"处理{inputs.Count}个宝可梦数据中……，大约需要{5*inputs.Count}秒", _channelId);
                        var result = GroupSeedFinder.FindSeed(inputs);
                        SendChannelAtMessage(eventBody.DodoId,
                            result is default(ulong) ? "没找到seed，请重新抓" : $"seed:{result}",
                            _channelId);
                    }
                    finally
                    {
                        Monitor.Exit(sync);
                    }
                }
                else
                {
                    SendChannelAtMessage(eventBody.DodoId, $"巨金怪的4个大脑都在计算中，请稍后再试", _channelId);
                }
            }
        }

        public void SendChannelAtMessage(string atDodoId, string message, string channelId)
        {
            if (string.IsNullOrEmpty(message)) return;
            _openApiService.SetChannelMessageSend(new SetChannelMessageSendInput<MessageBodyText>
            {
                ChannelId = channelId,
                MessageBody = new MessageBodyText
                {
                    Content = $"<@!{atDodoId}> {message}"
                }
            });
        }
    }
}