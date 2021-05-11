using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.Json;
using Dapper.Contrib.Extensions;

namespace Oops.DataModels
{
    [Table("error_log")]
    public class Error
    {

        public Error()
        {           
        }

        [Key]
        public int Id { get; set; }
        public string Application { get; set; }

        #region queued model's properties

        public string HostName { get; set; }
        public string TypeName { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
        public string Detail { get; set; }
        public string User { get; set; }
        public string Ip { get; set; }
        public string RealIp { get; set; }
        public DateTime Time { get; set; }

        public string HttpAgent { get; set; }
        public bool IsHttps { get; set; }
        public string HttpHost { get; set; }
        public string HttpPath { get; set; }
        public string HttpMethod { get; set; }
        public string HttpProtocol { get; set; }
        public int StatusCode { get; set; }

        [Computed]
        public List<DetailItem> QueryString { get; set; }
        [Computed]
        public List<DetailItem> Form { get; set; }
        [Computed]
        public List<DetailItem> Cookies { get; set; }
        [Computed]
        public List<DetailItem> Headers { get; set; }
        [Computed]
        public List<DetailItem> ServerValues { get; set; }

        #endregion

        public string QueryStringJson { get; set; }
        public string FormJson { get; set; }
        public string CookiesJson { get; set; }
        public string HeadersJson { get; set; }

        public void PrepareData()
        {
            this.QueryString = JsonSerializer.Deserialize<List<DetailItem>>(this.QueryStringJson);
            this.Form = JsonSerializer.Deserialize<List<DetailItem>>(this.FormJson);
            this.Cookies = JsonSerializer.Deserialize<List<DetailItem>>(this.CookiesJson);
            this.Headers = JsonSerializer.Deserialize<List<DetailItem>>(this.HeadersJson);
            this.ServerValues = new List<DetailItem>();

            this.ServerValues.Add(new DetailItem
            {
                Key = "HTTP_USER_AGENT",
                Value = this.HttpAgent
            });
            this.ServerValues.Add(new DetailItem
            {
                Key = "HTTPS",
                Value = this.IsHttps ? "on" : "off"
            });
            if (string.IsNullOrEmpty(this.RealIp) == false)
            {
                if (this.RealIp == this.Ip)
                {
                    this.ServerValues.Add(new DetailItem
                    {
                        Key = "REMOTE_HOST",
                        Value = this.RealIp
                    });
                }
                else
                {
                    this.ServerValues.Add(new DetailItem
                    {
                        Key = "REMOTE_HOST",
                        Value = this.RealIp + " from " + this.Ip
                    });
                }
            }
            this.ServerValues.Add(new DetailItem
            {
                Key = "REMOTE_USER",
                Value = this.User
            });
            this.ServerValues.Add(new DetailItem
            {
                Key = "REQUEST_METHOD",
                Value = this.HttpMethod
            });
            this.ServerValues.Add(new DetailItem
            {
                Key = "URL",
                Value = this.HttpPath            
            });
            this.ServerValues.Add(new DetailItem
            {
                Key = "HTTP_HOST",
                Value = this.HttpHost
            });
            this.ServerValues.Add(new DetailItem
            {
                Key = "STATUS_CODE",
                Value = this.StatusCode.ToString()
            });
            this.ServerValues.Add(new DetailItem
            {
                Key = "SERVE_BY",
                Value = this.HostName
            });
            this.ServerValues.Add(new DetailItem
            {
                Key = "APPLICATION",
                Value = this.Application
            });
        }

        public void PrppareJson()
        {
            this.QueryStringJson = JsonSerializer.Serialize(this.QueryString);
            this.FormJson = JsonSerializer.Serialize(this.Form);
            this.CookiesJson = JsonSerializer.Serialize(this.Cookies);
            this.HeadersJson = JsonSerializer.Serialize(this.Headers);
        }

        public class DetailItem
        {
            public string Key { get; set; }
            public string Value { get; set; }

            public override string ToString()
            {
                return string.Format("{0}: {1}", Key, Value);
            }
        }
    }
}
