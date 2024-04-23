using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TerroristChecker.Domain.Dice.Entities;

namespace TerroristChecker.Persistence.Configurations;

internal sealed class TerroristsConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("terrorists");

        builder.HasKey(person => person.Id);

        builder.Property(person => person.FullName)
            .HasMaxLength(255);
    }
}
