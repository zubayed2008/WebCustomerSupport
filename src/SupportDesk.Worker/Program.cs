using SupportDesk.Application;
using SupportDesk.Application.Abstractions;
using SupportDesk.Infrastructure;
using SupportDesk.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ICurrentUser, SystemCurrentUser>();
builder.Services.AddHostedService<SupportDeskBackgroundWorker>();

var host = builder.Build();
host.Run();

