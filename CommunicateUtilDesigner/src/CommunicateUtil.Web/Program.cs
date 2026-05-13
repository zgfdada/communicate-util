using CommunicateUtil.Generator;
using CommunicateUtil.Web.Data;
using CommunicateUtil.Web.Services;
using SqlSugar;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<DesignerOutputPaths>();
builder.Services.AddScoped<ICommunicationSchemaValidator, CommunicationSchemaValidator>();
builder.Services.AddScoped<ICommunicationCodeGenerator, CommunicationCodeGenerator>();
builder.Services.AddScoped<IDesignerRepository, DesignerRepository>();
builder.Services.AddScoped<ISqlSugarClient>(sp =>
{
    var paths = sp.GetRequiredService<DesignerOutputPaths>();
    return new SqlSugarClient(new ConnectionConfig
    {
        ConnectionString = $"Data Source={paths.DatabasePath}",
        DbType = DbType.Sqlite,
        IsAutoCloseConnection = true,
        InitKeyType = InitKeyType.Attribute
    });
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
    DesignerDatabase.Initialize(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Projects}/{action=Index}/{id?}");

app.Run();
