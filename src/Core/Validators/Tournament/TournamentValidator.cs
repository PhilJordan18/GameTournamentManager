using Core.DTOs;
using FluentValidation;

namespace Core.Validators.Tournament;

public class TournamentConfigValidator : AbstractValidator<TournamentConfig>
{
    public TournamentConfigValidator()
    {
        RuleFor(t => t.Name).NotEmpty().WithMessage("The name is required for the creation of a tournament")
            .MaximumLength(50).WithMessage("The name's lenght shouldn't exceed 50 characters");

        RuleFor(t => t.NbMaxPlayers)
            .GreaterThanOrEqualTo(4)
            .WithMessage("To create a tournament, you should dispose of at least 4 players")
            .LessThanOrEqualTo(32)
            .WithMessage("The number of players should be less than or equal to 32");
        
        RuleFor(t => t.BeginningDate)
            .LessThanOrEqualTo(DateTime.Now)
            .WithMessage("The beginning date must be in the past or today");

        RuleFor(t => t.EndDate)
            .GreaterThan(t => t.BeginningDate)
            .WithMessage("The end date must be after the beginning date");
    }
}