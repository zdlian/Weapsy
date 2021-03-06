﻿using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Weapsy.Domain.Languages;
using Weapsy.Domain.Pages;
using Weapsy.Infrastructure.Identity;
using Weapsy.Infrastructure.Queries;
using Weapsy.Reporting.Pages;
using Weapsy.Reporting.Pages.Queries;
using System.Linq;
using Weapsy.Data.Identity;

namespace Weapsy.Data.Reporting.Pages
{
    public class GetPageModuleAdminModelHandler : IQueryHandlerAsync<GetPageModuleAdminModel, PageModuleAdminModel>
    {
        private readonly IDbContextFactory _contextFactory;
        private readonly IRoleService _roleService;

        public GetPageModuleAdminModelHandler(IDbContextFactory contextFactory, IRoleService roleService)
        {
            _contextFactory = contextFactory;
            _roleService = roleService;
        }

        public async Task<PageModuleAdminModel> RetrieveAsync(GetPageModuleAdminModel query)
        {
            using (var context = _contextFactory.Create())
            {
                var page = GetPage(context, query.SiteId, query.PageId);

                if (page == null)
                    return null;

                var pageModule = page.PageModules.FirstOrDefault(x => x.Id == query.PageModuleId);

                if (pageModule == null)
                    return null;

                var result = new PageModuleAdminModel
                {
                    PageId = page.Id,
                    ModuleId = pageModule.ModuleId,
                    PageModuleId = pageModule.Id,
                    Title = pageModule.Title,
                    InheritPermissions = pageModule.InheritPermissions
                };

                var languages = await context.Languages
                    .Where(x => x.SiteId == query.SiteId && x.Status != LanguageStatus.Deleted)
                    .OrderBy(x => x.SortOrder)
                    .ToListAsync();

                foreach (var language in languages)
                {
                    var title = string.Empty;

                    var existingLocalisation = pageModule
                        .PageModuleLocalisations
                        .FirstOrDefault(x => x.LanguageId == language.Id);

                    if (existingLocalisation != null)
                    {
                        title = existingLocalisation.Title;
                    }

                    result.PageModuleLocalisations.Add(new PageModuleLocalisationAdminModel
                    {
                        PageModuleId = pageModule.Id,
                        LanguageId = language.Id,
                        LanguageName = language.Name,
                        LanguageStatus = language.Status,
                        Title = title
                    });
                }

                foreach (var role in _roleService.GetAllRoles())
                {
                    var pageModulePermission = new PageModulePermissionModel
                    {
                        RoleId = role.Id,
                        RoleName = role.Name,
                        Disabled = role.Name == DefaultRoleNames.Administrator
                    };

                    foreach (PermissionType permisisonType in Enum.GetValues(typeof(PermissionType)))
                    {
                        bool selected = pageModule.PageModulePermissions
                            .FirstOrDefault(x => x.RoleId == role.Id && x.Type == permisisonType) != null;

                        pageModulePermission.PageModulePermissionTypes.Add(new PageModulePermissionTypeModel
                        {
                            Type = permisisonType,
                            Selected = selected
                        });
                    }

                    result.PageModulePermissions.Add(pageModulePermission);
                }

                return result;
            }
        }

        private Entities.Page GetPage(WeapsyDbContext context, Guid siteId, Guid pageId)
        {
            var page = context.Pages
                .Include(x => x.PageLocalisations)
                .Include(x => x.PagePermissions)
                .FirstOrDefault(x => x.SiteId == siteId && x.Id == pageId && x.Status == PageStatus.Active);

            if (page == null)
                return null;

            page.PageModules = context.PageModules
                .Include(y => y.PageModuleLocalisations)
                .Include(y => y.PageModulePermissions)
                .Where(x => x.PageId == pageId && x.Status == PageModuleStatus.Active)
                .ToList();

            return page;
        }
    }
}
