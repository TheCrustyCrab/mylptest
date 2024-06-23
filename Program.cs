using LPApi;
using Highs2;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
var app = builder.Build();

app.UseCors(opts =>
{
    opts.AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin();
});

app.UseDefaultFiles();
app.UseStaticFiles();

//app.UseDeveloperExceptionPage();

app.MapPost("/highs/solve", (Problem problem) =>
{
    var watch = Stopwatch.StartNew();
    using var solver = new HighsLpSolver();
    var model = problem.ToHighsModel();
    HighsStatus status = solver.passLp(model);
    status = solver.run();
    HighsSolution sol = solver.getSolution();
    HighsModelStatus modelStatus = solver.GetModelStatus();
    watch.Stop();
    var result = new CalculationResult(watch.ElapsedMilliseconds, modelStatus == HighsModelStatus.kOptimal, sol.colvalue);
    return Results.Ok(result);
});

app.Run();
