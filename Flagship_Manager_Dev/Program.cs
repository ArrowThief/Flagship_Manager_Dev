using FlagShip_Manager;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.WebHost.UseUrls("http://*:8008");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

Thread WS = new Thread(new ThreadStart(() => WorkerServer.setupWorkerServer()));
Thread Manager = new Thread(() => jobManager.Manager());
WS.Start();
Manager.Start();

app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();
