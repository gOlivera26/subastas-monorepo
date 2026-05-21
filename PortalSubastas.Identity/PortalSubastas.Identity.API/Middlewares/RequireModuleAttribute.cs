using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PortalSubastas.Identity.API.Middlewares;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireModuleAttribute : TypeFilterAttribute
{
    public RequireModuleAttribute(string moduleKey) : base(typeof(RequireModuleFilter))
    {
        Arguments = new object[] { moduleKey };
    }
}

public class RequireModuleFilter : IAuthorizationFilter
{
    private readonly string _moduleKey;
    private readonly PortalSubastasContext _context;

    public RequireModuleFilter(string moduleKey, PortalSubastasContext context)
    {
        _moduleKey = moduleKey;
        _context = context;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>().Any())
            return;

        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var isSuperAdmin = user.IsInRole("SUPERADMIN");
        if (isSuperAdmin) return;

        var roleName = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(roleName))
        {
            context.Result = new ForbidResult();
            return;
        }

        var hasModuleAccess = _context.TRoles
            .Where(r => r.Nombre == roleName)
            .SelectMany(r => r.TRolesModulos)
            .Any(rm => rm.IdModuloNavigation.KeyName == _moduleKey);

        var hasPageAccess = _context.TRoles
            .Where(r => r.Nombre == roleName)
            .SelectMany(r => r.TRolesPaginas)
            .Any(rp => rp.IdPaginaNavigation.KeyName == _moduleKey);

        if (!hasModuleAccess && !hasPageAccess)
            context.Result = new ForbidResult();
    }
}
