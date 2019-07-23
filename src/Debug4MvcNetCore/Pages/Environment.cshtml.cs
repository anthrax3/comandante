﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Debug4MvcNetCore.Pages;
using Debug4MvcNetCore.PagesRenderer;
using Microsoft.AspNetCore.Mvc;

namespace Debug4MvcNetCore.Pages
{
    public class Environment : EmbeddedViewModel
    {
        public ServerInfoModel Model { get; set; }

        public override async Task InitView()
        {
            Model = new ServerInfoModel();
            Model.Environment = new EnvironmentService().Create();
            Model.Process = new ProcessService().GetProcessInfo();
        }
    }

    public class ServerInfoModel
    {
        public Environments Environment;
        public ProcessInfo Process;
    }

}
