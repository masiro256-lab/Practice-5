using Mapster;
using MapsterMapper;
using ProductCatalogApp.Api.Mapping;
using ProductCatalogApp.Api.Hubs;
using ProductCatalogApp.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using ProductCatalogApp.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer" });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Product Catalog API",
        Version = "v1"
    });
});

builder.Services.AddAuthentication(IdentityConstants.BearerScheme)
    .AddBearerToken(IdentityConstants.BearerScheme, options =>
    {
        options.BearerTokenExpiration = TimeSpan.FromHours(1);
        options.Events.OnMessageReceived = context =>
        {
            if (context.Request.Path.StartsWithSegments("/hubs") && context.Request.Query.TryGetValue("access_token", out var token))
                context.Token = token;
            return Task.CompletedTask;
        };
    });
builder.Services.AddIdentityCore<IdentityUser>(options =>
{
    options.Password.RequiredLength = 8;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddSignInManager();
builder.Services.AddAuthorization();

builder.Services.AddSignalR();
builder.Services.AddSingleton<OnlineConnections>();
builder.Services.AddScoped<ReadingService>();
builder.Services.AddHostedService<SensorSimulator>();
builder.Services.AddCors(options => options.AddPolicy("Client", policy => policy
    .WithOrigins(builder.Configuration["Cors:ClientOrigin"] ?? "http://localhost:5034")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var mapping = new TypeAdapterConfig();
new CatalogProfile().Register(mapping);
mapping.Compile();
builder.Services.AddSingleton(mapping);
builder.Services.AddScoped<IMapper, ServiceMapper>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
    DbSeeder.Seed(context);
    await MonitoringSeeder.SeedAsync(context);
    await IdentitySeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Catalog API v1");
});

app.UseCors("Client");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapControllers();
app.MapHub<MonitoringHub>("/hubs/monitoring").RequireAuthorization();

app.Run();
