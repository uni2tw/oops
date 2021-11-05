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
using Microsoft.Extensions.Options;
using Autofac.Core;
using Oops.Services.WebSockets;

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
        [Route("/api/test/log")]
        public dynamic MqttTestSendLog([FromQuery]string message)
        {
            var oopsClient = new OopsClient();
            try
            {
                oopsClient.Start("localhost", "test").Wait();
                oopsClient.PushLog("dev", "unk", 1 , "test", message);
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
        [Route("/api/test/error")]
        public dynamic MqttMessageTestError(string message)
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
            return Redirect("/oops/log.html");
            //return Redirect("https:/google.com");
        }

        [HttpGet]
        [Route("api/logs")]
        public async Task<dynamic> GetLogs([FromQuery] GetLogsModel model)
        {
            if (model == null)
            {
                return BadRequest();
            }
            int page = model.page ?? 1;
            int pageSize = model.page_size ?? 200;

            int currentPage = 0;
            int totalPages = 0;
            int totalRows = 0;
            try
            {
                OopsLogLevel minLevel = OopsLogLevel.Trace;
                OopsLogLevel maxLevel = OopsLogLevel.Off;
                if (model.warn_only == 1)
                {
                    minLevel = OopsLogLevel.Warn;
                }
                LogsResponse response = await new LogDao().GetLogs(
                    model.service, model.logger, minLevel, maxLevel, model.date, page, pageSize);
                
                currentPage = response.CurrentPage;
                totalPages = response.TotalPage;
                totalRows = response.TotalRows;

                int startRow = (currentPage - 1) * pageSize + 1;
                int endRow = currentPage* pageSize;
                if (endRow > totalRows) endRow = totalRows;
                return Ok(new
                {
                    response.Logs,
                    pagerInfo = new
                    {                        
                        currentPage,
                        totalPages,
                        totalRows,
                        startRow = startRow,
                        endRow = endRow
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());                
            }
        }

        [HttpGet]
        [Route("api/options")]
        public async Task<dynamic> GetOptions()
        {
            new LogDao().GetOptions(out List<string> services, out List<string> loggers, out List<string> dates);
            return Ok(new
            {
                services,
                loggers,
                dates
            });
        }

        [HttpGet]
        [Route("api/server_info")]
        public async Task<dynamic> GetServerInfo()
        {
            IMqttService mqttService = IoC.Get<IMqttService>();
            int providerNumber = mqttService.GetClientNumber();

            int viewerNumber = WebSocketEasyMiddleware.GetClientNumber();

            var data = new
            {
                ProviderNumber = providerNumber,
                viewerNumber = viewerNumber
            };
            return Ok(data);
        }

        [HttpPost]
        [Route("api/get_errors")]
        public async Task<dynamic> GetErrors([FromBody]GetErrorsModel model)
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
                ErrorsResponse response = await dao.GetLogs(model.app, page ?? 1, pageSize ?? 20);
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
        [Route("api/get_error")]
        public dynamic GetError([FromBody]GetErrorModel model)
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

        public class GetLogsModel
        {
            public string service { get; set; }
            public string logger { get; set; }
            public string date { get; set; }
            public int warn_only { get; set; }
            public int? page { get; set; }
            public int? page_size { get; set; }            
        }


        public class GetErrorsModel
        {
            public string app { get; set; }
            public int? page { get; set; }
            public int? pageSize { get; set; }
            public string sql { get; set; }
        }

        public class GetErrorModel
        {
            public int id { get; set; }
        }        
    }
}
