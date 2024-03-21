
using FlagShip_Manager;

//FileCheck.CheckFileSize("C:\\Users\\Nick\\Desktop\\Render Test\\4K", true);
//Console.ReadLine();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
//builder.Services.AddSingleton<WeatherForecastService>();
builder.WebHost.UseUrls("http://*:8008");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
Thread WS = new Thread(new ThreadStart(() => WorkerServer.setupWorkerServer()));
Thread Manager = new Thread(() => jobManager.Manager());
WS.Start();
Manager.Start();

//if (Debugger.IsAttached)
//{
//    jobManager.Manager("M:\\Render Watch folders\\Test\\RenderControl\\RenderCMD");
//}
//else
//jobManager.Manager("M:\\Render Watch folders\\RenderControl\\RenderCMD");

//FlagShip_Manager.Program.Main();
//app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");


app.Run();
