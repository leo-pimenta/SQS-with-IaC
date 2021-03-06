using App.Services;
using Infra.DI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
InfraDependencies.Inject(builder.Services);
builder.Services.AddSingleton<IMessageService, MessageService>();
builder.WebHost.UseKestrel(options => 
{
    options.ListenLocalhost(5002);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (Environment.GetEnvironmentVariable("SQS_QUEUE_URL") == null)
{
    throw new Exception("SQS_QUEUE_URL environment variable not set.");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
