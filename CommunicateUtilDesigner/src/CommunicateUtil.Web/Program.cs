using CommunicateUtil.Generator;
using CommunicateUtil.Web.Data;
using CommunicateUtil.Web.Services;
using SqlSugar;

var builder = WebApplication.CreateBuilder(args);

// 注册 MVC、路径服务、代码生成服务和数据仓储服务。
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<DesignerOutputPaths>();
builder.Services.AddScoped<ICommunicationSchemaValidator, CommunicationSchemaValidator>();
builder.Services.AddScoped<ICommunicationCodeGenerator, CommunicationCodeGenerator>();
builder.Services.AddScoped<IDesignerRepository, DesignerRepository>();
builder.Services.AddScoped<ISqlSugarClient>(sp =>
{
    var paths = sp.GetRequiredService<DesignerOutputPaths>();
    // SQLite 数据库文件放在 App_Data 下，SqlSugar 自动关闭连接以适配 Web 请求生命周期。
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
    // 应用启动时执行 CodeFirst 初始化，确保本地设计器数据库表结构存在。
    var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
    DesignerDatabase.Initialize(db);
}

// 配置 HTTP 请求管道。
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // 非开发环境启用 HSTS，减少浏览器降级到非 HTTPS 的风险。
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// 默认进入项目设计器列表，而不是模板项目的 Home 页面。
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Projects}/{action=Index}/{id?}");

app.Run();
