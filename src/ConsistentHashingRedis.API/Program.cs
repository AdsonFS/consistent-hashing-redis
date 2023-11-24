using ConsistentHashingRedis.Domain.ConsistentHashing;
using ConsistentHashingRedis.Infrastructure.ConsistentHashing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddSingleton<IConsistentHashing, ConsistentHashing>();
builder.Services.AddCors(o =>
{
    o.AddPolicy("CorsPolicy",
    builder => builder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("CorsPolicy");
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
