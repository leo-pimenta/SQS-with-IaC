using App.Services;
using Infra.Factories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.WebHost.UseKestrel();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

var messageService = new MessageService(MessageQueueFactory.Create());
var timer = new App.Services.Timer(TimeSpan.FromSeconds(1));
var continuousQueueReader = new ContinuousQueueReader(timer, messageService);
continuousQueueReader.Start();

app.Run();

