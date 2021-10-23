using System;
using System.Text;
using MQTTnet;
using MQTTnet.Server;
using Oops.Components;
using Oops.Daos;
using Oops.DataModels;

namespace Oops.Services
{
    public interface IMqttService
    {
        void Start();
        void Stop();
    }
    public class MqttService : IMqttService
    {
        public const string _BROADCAST_TOPIC_PREFIX = "broadcast-";
        public static System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> clientIds
            = new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>();
        IMqttServer mqttServer = new MqttFactory().CreateMqttServer();
        ErrorDao errorDao = IoC.Get<ErrorDao>();
        LogDao logDao = IoC.Get<LogDao>();

        public void Start()
        {
            Console.WriteLine("matt server starting.");

            IMqttServer mqttServer = new MqttFactory().CreateMqttServer();
            mqttServer.UseClientConnectedHandler(delegate (MqttServerClientConnectedEventArgs args)
            {
                if (clientIds.ContainsKey(args.ClientId) == false)
                {
                    clientIds.TryAdd(args.ClientId, DateTime.Now);
                }
            });
            mqttServer.UseClientDisconnectedHandler(delegate (MqttServerClientDisconnectedEventArgs args)
            {
                if (clientIds.ContainsKey(args.ClientId))
                {
                    DateTime temp;
                    clientIds.TryRemove(args.ClientId, out temp);
                }
            });            
            mqttServer.UseApplicationMessageReceivedHandler(
                async delegate (MqttApplicationMessageReceivedEventArgs eventArgs)
                {
                    string topic = eventArgs.ApplicationMessage.Topic;
                    if (topic != null && topic.StartsWith(_BROADCAST_TOPIC_PREFIX))
                    {
                        return;
                    }

                    if (topic == "log")
                    {
                        string payload =
                            Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload);

                        Log log = System.Text.Json.JsonSerializer.Deserialize<Log>(payload);
                        
                        Console.WriteLine("message: " + log.ToString());                        

                        await logDao.InsertLogAsaync(log);
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
            mqttServer.StartAsync(new MqttServerOptions()).Wait();

        }

        public void Stop()
        {
            Console.WriteLine("matt server stopping.");
            mqttServer.StopAsync().Wait();
        }
    }
}