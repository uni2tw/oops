using System;
using System.Text;
using MQTTnet;
using MQTTnet.Server;
using oops.DataModels;
using Oops.Components;
using Oops.Daos;
using Oops.DataModels;

namespace Oops.Services
{
    public delegate void OnChangedHandler(int number);
    public interface IMqttService
    {
        void Start(int? port);
        void Stop();
        int GetClientNumber();

        OnChangedHandler OnChanged { get; set; }
    }
    public class MqttService : IMqttService
    {
        public const string _BROADCAST_TOPIC_PREFIX = "broadcast-";
        public static System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> clientIds
            = new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>();
        IMqttServer mqttServer = new MqttFactory().CreateMqttServer();
        ErrorDao errorDao = IoC.Get<ErrorDao>();
        LogDao logDao = IoC.Get<LogDao>();

        public OnChangedHandler OnChanged { get; set; }

        public void Start(int? port)
        {
            Console.WriteLine("matt server starting.");

            IMqttServer mqttServer = new MqttFactory().CreateMqttServer();
            mqttServer.UseClientConnectedHandler(delegate (MqttServerClientConnectedEventArgs args)
            {
                if (clientIds.ContainsKey(args.ClientId) == false)
                {
                    clientIds.TryAdd(args.ClientId, DateTime.Now);
                }
                if (OnChanged != null)
                {
                    OnChanged(GetClientNumber());
                }
                Console.WriteLine($"client {args.ClientId} was connected.");
            });
            mqttServer.UseClientDisconnectedHandler(delegate (MqttServerClientDisconnectedEventArgs args)
            {
                if (clientIds.ContainsKey(args.ClientId))
                {
                    DateTime temp;
                    clientIds.TryRemove(args.ClientId, out temp);
                }
                if (OnChanged != null)
                {
                    OnChanged(GetClientNumber());
                }
                Console.WriteLine($"client {args.ClientId} was disconnected.");
            });
            mqttServer.UseApplicationMessageReceivedHandler(
                async delegate (MqttApplicationMessageReceivedEventArgs eventArgs)
                {
                    string topic = eventArgs.ApplicationMessage.Topic;
                    if (topic != null && topic.StartsWith(_BROADCAST_TOPIC_PREFIX))
                    {
                        return;
                    }

                    if (topic == OopsLog._TOPIC)
                    {
                        string payload =
                            Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload);

                        OopsLog log = System.Text.Json.JsonSerializer.Deserialize<OopsLog>(payload);

                        Console.Write(".");

                        //await logDao.InsertLogAsaync(log);
                        logDao.InsertLog(log);
                    }
                    else
                    {
                        string str =
                            Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload);

                        Error error = System.Text.Json.JsonSerializer.Deserialize<Error>(str);
                        error.PrppareJson();
                        error.Application = topic;
                        Console.WriteLine("message: " + error.Message);
                        Console.WriteLine("source: " + error.Source);
                        Console.WriteLine("host: " + error.HostName);
                        Console.WriteLine("time: " + error.Time);

                        await errorDao.InsertErrorLogAsync(error);
                    }
                });

            if (port != null)
            {
                var optionBuilder = new MqttServerOptionsBuilder()
                    .WithDefaultEndpointPort(port.Value);
                IMqttServerOptions option = optionBuilder.Build();
                mqttServer.StartAsync(option).Wait();
            }
            else
            {
                mqttServer.StartAsync(new MqttServerOptions()).Wait();
            }
        }

        public void Stop()
        {
            Console.WriteLine("matt server stopping.");
            mqttServer.StopAsync().Wait();
        }

        public int GetClientNumber()
        {
            return clientIds.Count;
        }
    }
}