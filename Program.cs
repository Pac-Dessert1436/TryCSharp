var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddHttpClient("TryCSharp", client =>
{
    var baseUrl = builder.Configuration["BaseUrl"];
    if (string.IsNullOrEmpty(baseUrl))
    {
        var serverHost = builder.Configuration["ServerHost"] ?? "0.0.0.0";
        var serverPort = builder.Configuration["ServerPort"] ?? "5001";
        var serverUrl = $"http://{serverHost}:{serverPort}";
        client.BaseAddress = new Uri(serverUrl);
    }
    else
    {
        client.BaseAddress = new Uri(baseUrl);
    }
});

builder.Services.AddScoped(sp => 
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("TryCSharp");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseCors("AllowAll");

app.MapRazorPages();
app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

var serverHost = builder.Configuration["ServerHost"] ?? "0.0.0.0";
var serverPort = builder.Configuration["ServerPort"] ?? "5001";
var serverUrl = $"http://{serverHost}:{serverPort}";
app.Run(serverUrl);