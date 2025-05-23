using WebApplication17.Executor;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddScoped<IExecutorRepository, ExecutorRepositoryMock>();
builder.Services.AddScoped<IExecutorService, ExecutorService>();
builder.Services.AddSingleton<IExecutorConfig, ExecutorConfig>();

var app = builder.Build();

// Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run("http://0.0.0.0:80");