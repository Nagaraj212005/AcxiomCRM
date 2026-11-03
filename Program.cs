using AcxiomCRM.Data;
using AcxiomCRM.Interfaces;
using AcxiomCRM.Repositories;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IdentityUiEmailSender = Microsoft.AspNetCore.Identity.UI.Services.IEmailSender;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Models;
using AcxiomCRM.Swagger;
using Microsoft.OpenApi;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AcxiomCRM REST API",
        Version = "v1",
        Description = """
            REST API for managing AcxiomCRM customers, leads, opportunities, and follow-ups.

            The API uses the application's ASP.NET Core Identity cookie authentication. Sign in to AcxiomCRM in this browser before using protected endpoints. Same-origin Swagger UI requests send the authentication cookie automatically.

            State-changing requests also require the `RequestVerificationToken` antiforgery header. Obtain a request token from a signed-in MVC form and send it with the request.
            """,
        Contact = new OpenApiContact
        {
            Name = "AcxiomCRM Support",
            Email = "support@acxiomcrm.local",
            Url = new Uri("https://localhost/")
        },
        License = new OpenApiLicense
        {
            Name = "Proprietary",
            Url = new Uri("https://localhost/")
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    options.TagActionsBy(description =>
    [
        description.GroupName ??
        description.ActionDescriptor.RouteValues["controller"] ??
        "API"
    ]);

    options.OperationFilter<ApiSecurityOperationFilter>();
});
builder.Services.AddTransient<IdentityUiEmailSender, SmtpEmailSender>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICrmService, CrmService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICustomerReportService, CustomerReportService>();
builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = !builder.Environment.IsDevelopment();
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CrmAccess", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("AdministratorOnly", policy => policy.RequireRole(CrmRoles.Admin));
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(5));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AcxiomCRM REST API v1");
        options.DocumentTitle = "AcxiomCRM API Documentation";
        options.DisplayRequestDuration();
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        options.EnableTryItOutByDefault();
    });
}

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
    await DevelopmentDataSeeder.SeedCustomersAsync(dbContext);
}
await using (var scope = app.Services.CreateAsyncScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in CrmRoles.All)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to create the required '{role}' role: {string.Join("; ", result.Errors.Select(error => error.Description))}");
            }
        }

    }

    var bootstrapEmail = builder.Configuration["BootstrapAdmin:Email"];
    var bootstrapPassword = builder.Configuration["BootstrapAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(bootstrapEmail) || !string.IsNullOrWhiteSpace(bootstrapPassword))
    {
        if (string.IsNullOrWhiteSpace(bootstrapEmail) || string.IsNullOrWhiteSpace(bootstrapPassword))
        {
            throw new InvalidOperationException("Both BootstrapAdmin:Email and BootstrapAdmin:Password must be configured together.");
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var admin = await userManager.FindByEmailAsync(bootstrapEmail);
        if (admin is null)
        {
            admin = new IdentityUser { UserName = bootstrapEmail, Email = bootstrapEmail, EmailConfirmed = true };
            var createResult = await userManager.CreateAsync(admin, bootstrapPassword);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to create the bootstrap administrator: {string.Join("; ", createResult.Errors.Select(error => error.Description))}");
            }
        }
        else if (!admin.EmailConfirmed)
        {
            admin.EmailConfirmed = true;
            var confirmationResult = await userManager.UpdateAsync(admin);
            if (!confirmationResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to confirm the bootstrap administrator email: {string.Join("; ", confirmationResult.Errors.Select(error => error.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, CrmRoles.Admin))
        {
            var roleResult = await userManager.AddToRoleAsync(admin, CrmRoles.Admin);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to assign the bootstrap administrator role: {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
            }
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/Home/NotFoundPage");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

app.Run();