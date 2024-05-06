using FluentValidation;

namespace TerroristChecker.Application.Cqrs.Queries.SearchTerrorists;

internal sealed class SearchTerroristsQueryValidator : AbstractValidator<SearchTerroristsQuery>
{
    public SearchTerroristsQueryValidator()
    {
        RuleFor(q => q.FullName)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("Full name should not be null or white space string");

        RuleFor(q => q.SearchOptions)
            .Must(x => x is null || x?.MinCoefficient is > 0 and <= 1)
            .WithMessage("Minimum coefficient should be more than zero and less or equal to one");


        RuleFor(q => q.SearchOptions)
            .Must(x => x is null || x?.MinAverageCoefficient is > 0 and <= 1)
            .WithMessage("Minimum average coefficient should be more than zero and less or equal to one");
    }
}
