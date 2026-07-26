using Backend.DTOs;

namespace Backend.Services.Optimiser;

public static class OptimiserResultFilter
{
    // Preference detail is private to its owner, everyone else sees counts only.
    public static SolveResponse ForReader(SolveResponse response, Guid readerId) =>
        response with
        {
            Solutions =
            [
                .. response.Solutions.Select(solution => solution with
                {
                    Score = solution.Score with
                    {
                        PerUser =
                        [
                            .. solution.Score.PerUser.Select(user =>
                                user.UserId == readerId
                                    ? user
                                    : user with
                                    {
                                        Satisfied = null,
                                        Violated = null,
                                    }
                            ),
                        ],
                    },
                }),
            ],
        };
}
