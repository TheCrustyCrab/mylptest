using LPApi;
using Highs2;
using System.Diagnostics;
using LpSolveDotNet;

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

LpSolve.Init();

app.MapPost("/{solver}/table", (Solver solver, TableProblem problem) =>
{
    // https://github.com/springer-math/linear-programming-using-MATLAB/blob/master/codes/benchmarkLPs/MPS
    var watch = Stopwatch.StartNew();
    switch (solver)
    {
        default:
        case Solver.highs:
            {
                using var highsSolver = new HighsLpSolver();
                highsSolver.setBoolOptionValue("log_to_console", 0);
                var model = problem.ToHighsModel();
                var status = highsSolver.passLp(model);
                status = highsSolver.run();
                var sol = highsSolver.getSolution();
                var modelStatus = highsSolver.GetModelStatus();
                var objectiveValue = highsSolver.getObjectiveValue();
                watch.Stop();
                var result = new CalculationResult(watch.ElapsedMilliseconds, modelStatus == HighsModelStatus.kOptimal, sol.colvalue, objectiveValue);
                return Results.Ok(result);
            }
        case Solver.lpsolve:
            {
                // todo
                watch.Stop();
                return Results.BadRequest();
            }
    }
    
});

app.MapPost("/{solver}/mps", (Solver solver, MPSProblem problem) =>
{
    // https://github.com/springer-math/linear-programming-using-MATLAB/blob/master/codes/benchmarkLPs/MPS
    using var tempFile = new TempFile(problem.Data, "mps");
    var watch = Stopwatch.StartNew();
    switch (solver)
    {
        default:
        case Solver.highs:
            {
                using var highsSolver = new HighsLpSolver();
                highsSolver.setBoolOptionValue("log_to_console", 0);
                highsSolver.readModel(tempFile.Name);
                var status = highsSolver.run();
                var sol = highsSolver.getSolution();
                var modelStatus = highsSolver.GetModelStatus();
                var objectiveValue = highsSolver.getObjectiveValue();
                watch.Stop();
                var result = new CalculationResult(watch.ElapsedMilliseconds, modelStatus == HighsModelStatus.kOptimal, sol.colvalue, objectiveValue);
                return Results.Ok(result);
            }
        case Solver.lpsolve:
            {
                using var lpSolver = LpSolve.read_MPS(tempFile.Name, lpsolve_verbosity.NORMAL, lpsolve_mps_options.MPS_FIXED);
                if (lpSolver == null)
                    return Results.BadRequest();
                var status = lpSolver.solve();
                var varCount = lpSolver.get_Ncolumns();
                var varCosts = new double[varCount];
                lpSolver.get_variables(varCosts);
                var objectiveValue = lpSolver.get_objective();
                watch.Stop();
                var result = new CalculationResult(watch.ElapsedMilliseconds, status == lpsolve_return.OPTIMAL, varCosts, objectiveValue);
                return Results.Ok(result);
            }            
    }    
});

app.Run();

enum Solver
{
    highs,
    lpsolve
}