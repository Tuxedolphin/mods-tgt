using Google.OrTools.Sat;
using Shouldly;

namespace Backend.Tests.Unit;

public class OrToolsSmokeTests
{
    [Fact]
    public void CpSat_TrivialModel_SolvesOptimal()
    {
        var model = new CpModel();
        var x = model.NewBoolVar("x");
        var y = model.NewBoolVar("y");
        model.Add(LinearExpr.Sum([x, y]) <= 1);
        model.Maximize(LinearExpr.WeightedSum([x, y], [2L, 1L]));

        var solver = new CpSolver { StringParameters = "num_search_workers:1,random_seed:42" };
        var status = solver.Solve(model);

        status.ShouldBe(CpSolverStatus.Optimal);
        solver.Value(x).ShouldBe(1);
        solver.Value(y).ShouldBe(0);
    }
}
