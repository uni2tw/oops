using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using oops.DataModels;
using Oops.DataModels;

namespace Oops.Components
{

    public class OopsClient
    {
        MqttFactory factory;
        MqttClient mqttClient;
        string topic;

        public async Task Start(string host, string topic)
        {
            factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient() as MqttClient;
            this.topic = topic ?? "Undefined";

            var options = new MqttClientOptionsBuilder()                
                .WithClientId(Environment.MachineName + "." + topic)
                .WithTcpServer(host)
                .Build();

            //重連
            mqttClient.UseDisconnectedHandler(async e =>
            {
            //Console.WriteLine("### disconnected from server ###");
            await Task.Delay(TimeSpan.FromSeconds(5));

                try
                {
                    await mqttClient.ConnectAsync(options, CancellationToken.None);
                }
                catch
                {
                //Console.WriteLine("### reconnecting failed ###");
            }
            });

            //開始連線
            try
            {
                await mqttClient.ConnectAsync(options, CancellationToken.None);
                // LogManager.GetLogger(typeof(OopsClient)).InfoFormat("連線 {0}/{1}",
                //     host, topic);
            }
            catch
            {
                // LogManager.GetLogger(typeof(OopsClient)).WarnFormat(
                //     "連線失敗 {0}/{1}", host, topic);
            }
        }

        public void Push(string pushMessage)
        {
            if (mqttClient != null)
            {
                try
                {
                    Error error = new Error();
                    error.HostName = "測試";
                    error.Message = pushMessage;
                    mqttClient.PublishAsync(this.topic, System.Text.Json.JsonSerializer.Serialize(error));
                }
                catch { }
            }
        }

        public void PushLog(string env, string service, int level,string logger, string message)
        {
            if (mqttClient != null)
            {
                try
                {
                    OopsLog log = new OopsLog();
                    log.Env = env;
                    log.Srv = service;
                    log.Host = Environment.MachineName;
                    log.Level = level;
                    log.Logger = logger;
                    log.Time = DateTime.Now;
                    log.DateHour = log.Time.ToString("yyyyMMddHH");
                    log.Date = log.Time.ToString("yyyyMMdd");
                    log.Message = message;
                    mqttClient.PublishAsync(DataModels.OopsLog._TOPIC, System.Text.Json.JsonSerializer.Serialize(log));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public void PushApi(string service, string path, string method,int statusCode, string request, string response, string error, string trace)
        {
            if (mqttClient != null)
            {
                try
                {
                    OopsApi api = new OopsApi(); 
   
                    api.Srv = service;
                    api.Host = Environment.MachineName;
                    api.Url = path;
                    api.Method = method;
                    api.StatusCode = statusCode;
                    api.Request = request;
                    api.Response = response;
                    api.Error = error;
                    api.Time = DateTime.Now;                    
                    api.Date = api.Time.ToString("yyyyMMdd");
         
                    mqttClient.PublishAsync(OopsApi._TOPIC, JsonSerializer.Serialize(api));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public void Stop()
        {
            if (mqttClient == null)
            {
                
                return;
            }
            mqttClient.Dispose();
        }
    }
}