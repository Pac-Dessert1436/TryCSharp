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

// TODO: Replace this with actual API URL when deploying
const string ApiBaseUrl = "http://localhost:5001/api/xxx";
builder.Services.AddHttpClient("TryCSharp", client =>
{
    client.BaseAddress = new Uri(ApiBaseUrl);
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

// Configure the application to listen on port 5001
app.Run($"http://*:5001");