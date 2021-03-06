﻿using System.Threading.Tasks;
using Weapsy.Domain.Languages;
using Weapsy.Domain.Menus;
using Weapsy.Infrastructure.Identity;
using Weapsy.Infrastructure.Queries;
using Weapsy.Reporting.Menus;
using Weapsy.Reporting.Menus.Queries;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Weapsy.Data.Identity;

namespace Weapsy.Data.Reporting.Menus
{
    public class GetDefaultItemForAdminHandler : IQueryHandlerAsync<GetDefaultItemForAdmin, MenuItemAdminModel>
    {
        private readonly IDbContextFactory _contextFactory;
        private readonly IRoleService _roleService;

        public GetDefaultItemForAdminHandler(IDbContextFactory contextFactory, IRoleService roleService)
        {
            _contextFactory = contextFactory;
            _roleService = roleService;
        }

        public async Task<MenuItemAdminModel> RetrieveAsync(GetDefaultItemForAdmin query)
        {
            using (var context = _contextFactory.Create())
            {
                var menu = await context.Menus.FirstOrDefaultAsync(x => x.SiteId == query.SiteId && x.Id == query.MenuId && x.Status != MenuStatus.Deleted);

                if (menu == null)
                    return new MenuItemAdminModel();

                var result = new MenuItemAdminModel();

                var languages = await context.Languages
                    .Where(x => x.SiteId == query.SiteId && x.Status != LanguageStatus.Deleted)
                    .OrderBy(x => x.SortOrder)
                    .ToListAsync();

                foreach (var language in languages)
                {
                    result.MenuItemLocalisations.Add(new MenuItemAdminModel.MenuItemLocalisation
                    {
                        LanguageId = language.Id,
                        LanguageName = language.Name,
                        LanguageStatus = language.Status,
                        Text = string.Empty,
                        Title = string.Empty
                    });
                }

                foreach (var role in _roleService.GetAllRoles())
                {
                    result.MenuItemPermissions.Add(new MenuItemAdminModel.MenuItemPermission
                    {
                        RoleId = role.Id,
                        RoleName = role.Name,
                        Selected = role.Name == DefaultRoleNames.Administrator,
                        Disabled = role.Name == DefaultRoleNames.Administrator
                    });
                }

                return result;
            }
        }
    }
}
