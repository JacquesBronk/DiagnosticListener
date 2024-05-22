using Diagnostics.Lib;
using Diagnostics.Lib.Domain;
using SimpleWeb.Api.Job;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDiagnostics();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/dojob",  async (ILogger<SomeRandomJobTask> logger, DiagnosticWrapper<SomeRandomJobTask> diagnosticWrapper, CancellationToken cancellationToken) =>
    {
        SomeRandomJobTask job = new(logger,diagnosticWrapper);
        string? jobResult = await job.ExecuteAsync(cancellationToken);
        return jobResult ?? "Job Failed";
    })
    .WithName("DoJob")
    .WithOpenApi();

app.Run();

