using WSMSimulator.HostedServices;
using WSMSimulator.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDB"));
builder.Services.Configure<WaterPumpUsageSettings>(builder.Configuration.GetSection("WaterPumpUsage"));
builder.Services.Configure<WaterUsageSettings>(builder.Configuration.GetSection("WaterUsage"));
builder.Services.Configure<ChemicalUsageSettings>(builder.Configuration.GetSection("ChemicalUsage"));

// Hosted service
builder.Services.AddHostedService<WaterPumpUsageHostedService>();
builder.Services.AddHostedService<WaterUsageHostedService>();
builder.Services.AddHostedService<ChemicalUsageHostedService>();

//builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

builder.WebHost.UseUrls(builder.Configuration.GetValue<string>("Urls").Split(";"));

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

app.Run();
