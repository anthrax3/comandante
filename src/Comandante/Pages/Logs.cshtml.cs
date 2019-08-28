﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Comandante.Pages;
using Comandante.PagesRenderer;
using Microsoft.AspNetCore.Mvc;

namespace Comandante.Pages
{
    public class Logs : EmbeddedViewModel
    {
        private RequestsService _requestsService = new RequestsService();

        public LogsModel Model { get; set; }

        public override async Task<EmbededViewResult> InitView()
        {
            Model = new LogsModel();
            Model.Logs = _requestsService.AllLogs.Take(1000).ToList();
            return await View();
        }
    }

    public class LogsModel
    {
        public IEnumerable<LogEntry> Logs { get; set; }

    }

}