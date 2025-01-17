﻿using System;
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

                var inputs = GroupSeedFinder.GetInputsFromText(content);

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
                            result.FirstIndex == -1 ? "没找到seed，请重新抓" : $"从第{result.FirstIndex + 1}只宝可梦算出了seed:{result.Seed}",
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