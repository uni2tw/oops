using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Oops.Components;
using Oops.Daos;
using Oops.Services;
using Oops.DataModels;
using Oops.ViewModels;
using System.Threading.Tasks;

namespace Oops.WebAPIs
{
    public class HomeAPIController : Controller
    {
        ErrorDao dao = IoC.Get<ErrorDao>();

        [HttpGet]
        [Route("/api/info")]
        public dynamic Info()
        {
            return new
            {
                platform = Environment.OSVersion.VersionString,
                user = Environment.UserName,
                machine = Environment.MachineName,
                time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        [HttpGet]
        [Route("/api/mqtt")]
        public dynamic MqttMessageTest(string message)
        {
            var oopsClient = new OopsClient();            
            try
            {

                oopsClient.Start("localhost", "test").Wait();
                oopsClient.Push(message);
                oopsClient.Stop();                
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new
            {

                message = message
            };

        }

        [HttpGet]
        [Route("")]
        public dynamic Home()
        {
            return Redirect("/oops/index.html");
            //return Redirect("https:/google.com");
        }
        [HttpPost]
        [Route("api/getLogs")]
        public async Task<dynamic> GetLogs([FromBody]GetLogsModel model)
        {
            if (model == null)
            {
                return BadRequest();
            }
            int? page = model.page;
            int? pageSize = model.pageSize;

            int currentPage = 0;
            int totalPage = 0;
            int totalRows = 0;
            List<ErrorModel> logs;
            if (string.IsNullOrEmpty(model.sql))
            {
                var response = await dao.GetLogs(model.app, page ?? 1, pageSize ?? 20);
                logs = response.Errors
                    .Select(t => new ErrorModel
                    {
                        id = t.Id,
                        app = t.Application,
                        host = t.HostName,
                        type = t.Source ?? t.TypeName,
                        url = t.HttpPath,
                        user = t.User,
                        error = t.Message,
                        code = t.StatusCode.ToString(),
                        date = t.Time.ToString("yyyy/MM/dd"),
                        time = t.Time.ToString("HH:mm:ss")
                    }).ToList();
                currentPage = response.CurrentPage;
                totalPage = response.TotalPage;
                totalRows = response.TotalRows;
            } 
            else
            {
                var response = await dao.GetLogsBySql(model.sql);
                logs = response.Errors
                    .Select(t => new ErrorModel
                    {
                        id = t.Id,
                        app = t.Application,
                        host = t.HostName,
                        type = t.Source ?? t.TypeName,
                        url = t.HttpPath,
                        user = t.User,
                        error = t.Message,
                        code = t.StatusCode.ToString(),
                        date = t.Time.ToString("yyyy/MM/dd"),
                        time = t.Time.ToString("HH:mm:ss")
                    }).ToList();
                
                currentPage = response.CurrentPage;
                totalPage = response.TotalPage;
                totalRows = response.TotalRows;
            }
            return Ok(new { logs, pageInfo = new { currentPage, totalPage, totalRows } });
        }

        [HttpPost]
        [Route("api/getLog")]
        public dynamic GetLog([FromBody]GetLogModel model)
        {
            if (model == null || model.id == 0)
            {
                return BadRequest();
            }
            Error error = dao.GetLog(model.id);
            ErrorDetailModel log = new ErrorDetailModel
            {
                id = error.Id,
                app = error.Application,
                host = error.HostName,
                type = error.Source,
                url = error.HttpPath,
                user = error.User,
                error = error.Message,
                detail = error.Detail,
                code = error.StatusCode.ToString(),
                serverValues = error.ServerValues,
                headers = error.Headers,
                querys = error.QueryString,
                forms = error.Form ,
                cookies = error.Cookies,
                date = error.Time.ToString("yyyy/MM/dd"),
                time = error.Time.ToString("HH:mm:ss")
            };
            return Ok(log);
        }

        [HttpGet]
        [Route("api/getApplications")]
        public dynamic GetApplications([FromQuery]int? startIndex, [FromQuery]int? pageSize)
        {
            List<string> applications = dao.GetApplications();
            return Ok(applications);
        }

        // [HttpGet]
        // [Route("api/clients")]
        // public dynamic GetGetClients()
        // {
        //     List<string> clients = Program.clientIds.Keys.ToList();
        //     return Ok(clients);
        // }

        public class GetLogsModel
        {
            public string app { get; set; }
            public int? page { get; set; }
            public int? pageSize { get; set; }
            public string sql { get; set; }
        }

        public class GetLogModel
        {
            public int id { get; set; }
        }        
    }
}
