﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Comandante.Pages;
using Comandante.PagesRenderer;
using Comandante.Services;
using Microsoft.AspNetCore.Mvc;

namespace Comandante.Pages
{
    public class EFEntityEditor : EmbeddedViewModel
    {
        public EFEntityEditorModel Model { get; set; }

        public override async Task<EmbededViewResult> InitView()
        {
            Model = new EFEntityEditorModel();

            var contextName = this.HttpContext.Request.Query.FirstOrDefault(x => x.Key == "_dbContext").Value.ToString().Trim();
            var entityName = this.HttpContext.Request.Query.FirstOrDefault(x => x.Key == "_entity").Value.ToString().Trim();
            var entityInfo = new EntityFrameworkService()
                .GetAppDbContexts(this.HttpContext)
                .FirstOrDefault(x => x.Name == contextName)
                .Entities
                .FirstOrDefault(x => x.ClrTypeName == entityName);
            
            var fieldsValues = new Dictionary<string, string>();
            var isSubmit = false;

            var pkValues = this.HttpContext.Request.Query
                .Where(x => x.Key.StartsWith("_") == false && string.IsNullOrEmpty(x.Value.FirstOrDefault()) == false)
                .ToDictionary(x => x.Key, x => x.Value.FirstOrDefault()?.ToString());
            var isUpdate = pkValues.Count > 0;

            if (string.Equals(this.HttpContext.Request.Method, "POST", StringComparison.CurrentCultureIgnoreCase))
            {
                fieldsValues = this.HttpContext.Request.Form
                    .Where(x => x.Key.StartsWith("_") == false && string.IsNullOrEmpty(x.Value.FirstOrDefault()) == false)
                    .ToDictionary(x => x.Key, x => x.Value.FirstOrDefault()?.ToString());
                isSubmit = this.HttpContext.Request.Form.Any(x => x.Key == "_submit");
                
            }
            
            if (string.Equals(this.HttpContext.Request.Method, "GET", StringComparison.CurrentCultureIgnoreCase))
            {
                var entity = new EntityFrameworkService().GetByPrimaryKey(this.HttpContext, contextName, entityInfo, pkValues);
                if (entity != null)
                    fieldsValues = entityInfo.Fields.ToDictionary(x => x.Name, x => entity.GetPropertyOrFieldValue(x.Name)?.ToString());
            }

            Model.AppDbContext = contextName;
            Model.Entity = entityInfo;
            Model.FieldsWithValues = entityInfo.Fields.Select(x => (x, fieldsValues.FirstOrDefault(v => v.Key == x.Name).Value)).ToList();

            if (isSubmit)
            {
                if (isUpdate)
                    new EntityFrameworkService().Update(this.HttpContext, contextName, entityInfo, pkValues, fieldsValues);
                else
                    new EntityFrameworkService().Add(this.HttpContext, contextName, entityInfo, fieldsValues);
            }

            return await View();
        }
    }

    public class EFEntityEditorModel
    {
        public string AppDbContext;
        public AppDbContextEntityInfo Entity;
        public List<(AppDbContextEntityFieldInfo Field, string Value)> FieldsWithValues;
    }

   
}
