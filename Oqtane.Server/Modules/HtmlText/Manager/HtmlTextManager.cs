using Oqtane.Infrastructure;
using Oqtane.Models;
using Oqtane.Modules.HtmlText.Repository;
using System.Net;
using Oqtane.Enums;
using Oqtane.Repository;
using Oqtane.Shared;
using Oqtane.Migrations.Framework;
using Oqtane.Documentation;
using System.Linq;
using Oqtane.Interfaces;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

// ReSharper disable ConvertToUsingDeclaration

namespace Oqtane.Modules.HtmlText.Manager
{
    [PrivateApi("Mark HtmlText classes as private, since it's not very useful in the public docs")]
    public class HtmlTextManager : MigratableModuleBase, IInstallable, IPortable, ISearchable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHtmlTextRepository _htmlText;
        private readonly IDBContextDependencies _DBContextDependencies;
        private readonly ISqlRepository _sqlRepository;

        public HtmlTextManager(
            IServiceProvider serviceProvider,
            IHtmlTextRepository htmlText,
            IDBContextDependencies DBContextDependencies,
            ISqlRepository sqlRepository)
        {
            _serviceProvider = serviceProvider;
            _htmlText = htmlText;
            _DBContextDependencies = DBContextDependencies;
            _sqlRepository = sqlRepository;
        }

        public string ExportModule(Module module)
        {
            string content = "";
            var htmltexts = _htmlText.GetHtmlTexts(module.ModuleId);
            if (htmltexts != null && htmltexts.Any())
            {
                var htmltext = htmltexts.OrderByDescending(item => item.CreatedOn).First();
                content = WebUtility.HtmlEncode(htmltext.Content);
            }
            return content;
        }

        public async Task<List<SearchContent>> GetSearchContentsAsync(PageModule pageModule, DateTime lastIndexedOn)
        {
            await Task.CompletedTask;

            var searchContentList = new List<SearchContent>();

            var htmltexts = _htmlText.GetHtmlTexts(pageModule.ModuleId);
            if (htmltexts != null && htmltexts.Any())
            {
                var htmltext = htmltexts.OrderByDescending(item => item.CreatedOn).First();
                if (htmltext.CreatedOn >= lastIndexedOn)
                {
                    searchContentList.Add(new SearchContent
                    {
                        Title = pageModule.Module.Title,
                        Description = string.Empty,
                        Body = htmltext.Content,
                        ContentModifiedBy = htmltext.ModifiedBy,
                        ContentModifiedOn = htmltext.ModifiedOn
                    });
                }
            }

            return searchContentList;
        }

        public void ImportModule(Module module, string content, string version)
        {
            content = WebUtility.HtmlDecode(content);
            var htmlText = new Models.HtmlText();
            htmlText.ModuleId = module.ModuleId;
            htmlText.Content = content;
            _htmlText.AddHtmlText(htmlText);
        }

        public bool Install(Tenant tenant, string version)
        {
            if (tenant.DBType == Constants.DefaultDBType && version == "1.0.1")
            {
                // version 1.0.0 used SQL scripts rather than migrations, so we need to seed the migration history table
                _sqlRepository.ExecuteNonQuery(tenant, MigrationUtils.BuildInsertScript("HtmlText.01.00.00.00"));
            }
            return Migrate(new HtmlTextContext(_DBContextDependencies), tenant, MigrationType.Up);
        }

        public bool Uninstall(Tenant tenant)
        {
            return Migrate(new HtmlTextContext(_DBContextDependencies), tenant, MigrationType.Down);
        }
    }
}
