﻿using Debug4MvcNetCore.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Debug4MvcNetCore
{
    public class RequestsService
    {
        private static List<RequestResponseInfo> _requests = new List<RequestResponseInfo>();
        public IEnumerable<RequestResponseInfo> Requests
        {
            get { return _requests; }
        }

        private static List<LogEntry> _webHostlogs = new List<LogEntry>();
        public IEnumerable<LogEntry> Logs
        {
            get { return _requests.SelectMany(x => x.Logs).ToList().Union(_webHostlogs).ToList().OrderByDescending(x => x.Created).ToList(); }
        }

        public void StartRequest(HttpContext httpContext)
        {
            if (ConfigurationInfo.EnableRequestLogs == false)
                return;

            var requestResponseInfo = CreateRequestResponseInfo(httpContext);
            httpContext.Items["Debug4MvcNetCore_Request"] = requestResponseInfo;
        }

        public void EndRequest(HttpContext httpContext)
        {
            if (ConfigurationInfo.EnableRequestLogs == false)
                return;

            LogUnhandledException(httpContext);

            var request = ((RequestResponseInfo)httpContext.Items["Debug4MvcNetCore_Request"]);
            request.Response = CreateResponseInfo(httpContext);
            request.Identities = CreateIdentityInfo(httpContext);
            request.Completed = DateTime.UtcNow;
            _requests.Insert(0, request);

            if (ConfigurationInfo.EnableRequestLogsOnlyIfError)
                _requests = _requests.Where(x => x.IsError).OrderByDescending(x => x.Created).Take(ConfigurationInfo.MaxNumberOfRequestsLogs).ToList();

            if (ConfigurationInfo.EnableRequestLogsOnlyIfAspMvc)
                _requests = _requests.Where(x => x.IsAspNetCore).OrderByDescending(x => x.Created).Take(ConfigurationInfo.MaxNumberOfRequestsLogs).ToList();
            
            _requests = _requests.OrderByDescending(x => x.Created).Take(ConfigurationInfo.MaxNumberOfRequestsLogs).ToList();
        }

        private static void LogUnhandledException(HttpContext httpContext)
        {
            var exceptionHandlerPathFeature = httpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionHandlerPathFeature?.Error != null)
            {
                var logger = (ILogger<Debug4MvcNetCoreLoggerMiddleware>)httpContext.RequestServices.GetService(typeof(ILogger<Debug4MvcNetCoreLoggerMiddleware>));
                if (logger != null)
                    logger.LogError(exceptionHandlerPathFeature.Error.Message, exceptionHandlerPathFeature.Error);
            }
        }

        public void AddLog<TState>(string loggerName, LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var httpContext = new HttpContextHelper().HttpContext;
            bool isWebHostLog = httpContext == null;

            if (isWebHostLog)
            {
                if (ConfigurationInfo.EnableWebHostLogs == false)
                    return;
            }
            else
            {
                if (ConfigurationInfo.EnableRequestLogs == false)
                    return;
            }

            var details = formatter(state, exception);
            var exceptionDetails = "";
            while (exception != null)
            {
                details = details + " \n " + exception.Message;
                exceptionDetails = exceptionDetails + (exception.Message + " \n " + exception.StackTrace);
                exception = exception.InnerException;
            }

            var logEntry = new LogEntry
            {
                Id = Guid.NewGuid().ToString(),
                IdentityName = httpContext?.User?.Identity?.Name,
                TraceIdentifier = httpContext?.TraceIdentifier,
                LogLevel = logLevel,
                EventId = eventId.Id,
                LoggerName = loggerName,
                Details = details,
                Exception = exceptionDetails,
                Created = DateTime.UtcNow,
                RequestUrl = string.Concat(httpContext?.Request?.Method, " ", httpContext?.Request?.Path),
            };

            if (isWebHostLog)
            {
                _webHostlogs.Insert(0, logEntry);
                _webHostlogs = _webHostlogs.Take(ConfigurationInfo.MaxNumberOfWebHostLogs).ToList();
            }
            else
            {
                var request = ((RequestResponseInfo)httpContext.Items["Debug4MvcNetCore_Request"]);
                request.Logs.Insert(0, logEntry);
            }
        }

        public void ClearLogs()
        {
            _webHostlogs.Clear();
            _requests.Clear();
        }

        public RequestResponseInfo CreateRequestResponseInfo(HttpContext httpContext)
        {
            var now = DateTime.Now;
            var nowUtc = now.ToUniversalTime();
            RequestResponseInfo requestResponseInfo = new RequestResponseInfo();
            requestResponseInfo.Request = CreateRequestInfo(httpContext);
            requestResponseInfo.Response = CreateResponseInfo(httpContext);
            requestResponseInfo.Created = now;
            requestResponseInfo.TraceIdentifier = httpContext.TraceIdentifier;
            requestResponseInfo.DateTime.Now = now.ToString("yyyy-MM-ddTHH:mm:sszzz");
            requestResponseInfo.DateTime.NowUtc = nowUtc.ToString("yyyy-MM-ddTHH:mm:sszzz");
            requestResponseInfo.LocalTimeZone.Id = TimeZoneInfo.Local.Id;
            requestResponseInfo.LocalTimeZone.DisplayName = TimeZoneInfo.Local.DisplayName;
            requestResponseInfo.LocalTimeZone.StandardName = TimeZoneInfo.Local.StandardName;
            requestResponseInfo.LocalTimeZone.BaseUtcOffset = TimeZoneInfo.Local.BaseUtcOffset.ToString();
            requestResponseInfo.LocalTimeZone.NowUtcOffset = TimeZoneInfo.Local.GetUtcOffset(now).ToString();
            requestResponseInfo.LocalTimeZone.SupportsDaylightSavingTime = TimeZoneInfo.Local.SupportsDaylightSavingTime;
            requestResponseInfo.UtcTimeZone.Id = TimeZoneInfo.Utc.Id;
            requestResponseInfo.UtcTimeZone.DisplayName = TimeZoneInfo.Utc.DisplayName;
            requestResponseInfo.UtcTimeZone.StandardName = TimeZoneInfo.Utc.StandardName;
            requestResponseInfo.UtcTimeZone.BaseUtcOffset = TimeZoneInfo.Utc.BaseUtcOffset.ToString();
            requestResponseInfo.UtcTimeZone.NowUtcOffset = TimeZoneInfo.Utc.GetUtcOffset(now).ToString();
            requestResponseInfo.UtcTimeZone.SupportsDaylightSavingTime = TimeZoneInfo.Utc.SupportsDaylightSavingTime;
            requestResponseInfo.CurrentCulture.Name = Thread.CurrentThread.CurrentCulture?.Name;
            requestResponseInfo.CurrentCulture.DisplayName = Thread.CurrentThread.CurrentCulture?.DisplayName;
            requestResponseInfo.CurrentCulture.EnglishName = Thread.CurrentThread.CurrentCulture?.EnglishName;
            requestResponseInfo.CurrentCulture.DateTimeFormat = Thread.CurrentThread.CurrentCulture?.DateTimeFormat?.ShortDatePattern + " " + Thread.CurrentThread.CurrentCulture?.DateTimeFormat?.ShortTimePattern;
            requestResponseInfo.CurrentUICulture.Name = Thread.CurrentThread.CurrentUICulture?.Name;
            requestResponseInfo.CurrentUICulture.DisplayName = Thread.CurrentThread.CurrentUICulture?.DisplayName;
            requestResponseInfo.CurrentUICulture.EnglishName = Thread.CurrentThread.CurrentUICulture?.EnglishName;
            requestResponseInfo.CurrentUICulture.DateTimeFormat = Thread.CurrentThread.CurrentUICulture?.DateTimeFormat?.ShortDatePattern + " " + Thread.CurrentThread.CurrentUICulture?.DateTimeFormat?.ShortTimePattern;
            requestResponseInfo.Connection.Id = httpContext.Connection.Id;
            requestResponseInfo.Connection.LocalIpAddress = httpContext.Connection.LocalIpAddress?.ToString();
            requestResponseInfo.Connection.LocalPort = httpContext.Connection.LocalPort;
            requestResponseInfo.Connection.RemoteIpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            requestResponseInfo.Connection.RemotePort = httpContext.Connection.RemotePort;
            requestResponseInfo.Connection.ClientCertificate = httpContext.Connection.ClientCertificate?.ToString();
            requestResponseInfo.Identities = CreateIdentityInfo(httpContext);

            return requestResponseInfo;
        }

        public RequestInfo CreateRequestInfo(HttpContext httpContext)
        {
            RequestInfo requestInfo = new RequestInfo();
            requestInfo.QueryString = httpContext.Request.QueryString.Value;
            requestInfo.Host = httpContext.Request.Host.ToString();
            requestInfo.Method = httpContext.Request.Method;
            requestInfo.Scheme = httpContext.Request.Scheme;
            requestInfo.Protocol = httpContext.Request.Protocol;
            requestInfo.Path = httpContext.Request.Path;
            requestInfo.PathBase = httpContext.Request.PathBase;
            try
            {
                requestInfo.Form = httpContext.Request.Form.ToDictionary(x => x.Key, x => x.Value.ToArray());
            }
            catch { }
            requestInfo.Headers = httpContext.Request.Headers.ToDictionary(x => x.Key, x => x.Value.Select(v => v).ToArray());
            requestInfo.Cookies = httpContext.Request.Cookies.ToDictionary(x => x.Key, x => x.Value.ToString());
            return requestInfo;
        }

        public ResponseInfo CreateResponseInfo(HttpContext httpContext)
        {
            ResponseInfo responseInfo = new ResponseInfo();
            responseInfo.StatusCode = httpContext.Response.StatusCode;
            responseInfo.Headers = httpContext.Response.Headers.ToDictionary(x => x.Key, x => x.Value.Select(v => v).ToArray());
            return responseInfo;
        }

        public IdentityDebugInfo[] CreateIdentityInfo(HttpContext httpContext)
        {
            return httpContext
                            .User
                            .Identities
                            .Select(i =>
                                new IdentityDebugInfo
                                {
                                    Name = i.Name,
                                    AuthenticationType = i.AuthenticationType,
                                    IsAuthenticated = i.IsAuthenticated,
                                    Label = i.Label,
                                    Actor = i.Name,
                                    NameClaimType = i.NameClaimType,
                                    RoleClaimType = i.RoleClaimType,
                                    Claims = i.Claims.Select(c =>
                                        new IdentityClaimDebugInfo
                                        {
                                            Issuer = c.Issuer,
                                            OriginalIssuer = c.OriginalIssuer,
                                            Subject = c.Subject?.ToString(),
                                            Type = c.Type,
                                            Value = c.Value,
                                            ValueType = c.ValueType,
                                            Properties = c.Properties.ToDictionary(p => p.Key, p => p.Value),
                                        }).ToArray()
                                })
                            .ToArray();
        }
    }

    public class RequestResponseInfo
    {
        public string TraceIdentifier;
        public RequestInfo Request = new RequestInfo();
        public ResponseInfo Response = new ResponseInfo();
        public DateTimeDebugInfo DateTime = new DateTimeDebugInfo();
        public ConnectionDebugInfo Connection = new ConnectionDebugInfo();
        public IdentityDebugInfo[] Identities = new IdentityDebugInfo[0];
        public IdentityDebugInfo Identity => Identities.FirstOrDefault();
        public TimeZoneDebugInfo LocalTimeZone = new TimeZoneDebugInfo();
        public TimeZoneDebugInfo UtcTimeZone = new TimeZoneDebugInfo();
        public CultureDebugInfo CurrentCulture = new CultureDebugInfo();
        public CultureDebugInfo CurrentUICulture = new CultureDebugInfo();
        public DateTime Created;
        public List<LogEntry> Logs = new List<LogEntry>();

        public bool IsError
        {
            get { return MaxLogLevel >= LogLevel.Error || Response?.StatusCode == 500;  }
        }

        public bool IsAspNetCore
        {
            get { return Logs.Any(x => x.LoggerName != null && (x.LoggerName.StartsWith("Microsoft.AspNetCore.Mvc.") || x.LoggerName.StartsWith("	Microsoft.AspNetCore.Routing."))); }
        }

        public LogLevel MaxLogLevel
        {
            get { return Logs.Count > 0 ? Logs.Max(x => x.LogLevel) : LogLevel.None; }
        }

        public DateTime Completed { get; internal set; }
    }

    public class LogEntry
    {
        public string Id { get; set; }
        public LogLevel LogLevel;
        public int EventId;
        public string LoggerName;
        public string Details;
        public string IdentityName;
        public string TraceIdentifier;
        public DateTime Created;
        public string Exception;
        public string RequestUrl;
    }

    public class RequestInfo
    {
        public string QueryString;
        public string Host;
        public string Method;
        public string Scheme;
        public string Protocol;
        public string Path;
        public string PathBase;
        public Dictionary<string, string[]> Headers = new Dictionary<string, string[]>();
        public Dictionary<string, string> Cookies = new Dictionary<string, string>();
        public Dictionary<string, string[]> Form = new Dictionary<string, string[]>();
    }

    public class ResponseInfo
    {
        public int StatusCode;
        public Dictionary<string, string[]> Headers;
    }

    public class DateTimeDebugInfo
    {
        public string Now;
        public string NowUtc;
    }

    public class TimeZoneDebugInfo
    {
        public string Id;
        public string DisplayName;
        public string StandardName;
        public string NowUtcOffset;
        public string BaseUtcOffset;
        public bool SupportsDaylightSavingTime;
    }

    public class CultureDebugInfo
    {
        public string Name;
        public string DisplayName;
        public string EnglishName;
        public string DateTimeFormat;
    }

    public class IdentityDebugInfo
    {
        public string Name;
        public string AuthenticationType;
        public bool IsAuthenticated;
        public string Label;
        public string Actor;
        public string NameClaimType;
        public string RoleClaimType;
        public IdentityClaimDebugInfo[] Claims = new IdentityClaimDebugInfo[0];
    }

    public class IdentityClaimDebugInfo
    {
        public string Subject;
        public string Type;
        public string Value;
        public string ValueType;
        public string Issuer;
        public string OriginalIssuer;
        public Dictionary<string, string> Properties = new Dictionary<string, string>();
    }

    public class ConnectionDebugInfo
    {
        public string Id;
        public string LocalIpAddress;
        public int LocalPort;
        public string RemoteIpAddress;
        public int RemotePort;
        public string ClientCertificate;
    }
}
