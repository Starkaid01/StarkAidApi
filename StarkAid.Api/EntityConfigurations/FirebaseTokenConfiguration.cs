using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarkAid.Api.Entities;

namespace StarkAid.Api.EntityConfigurations;

public class FirebaseTokenConfiguration : IEntityTypeConfiguration<FirebaseToken>
{
    public void Configure(EntityTypeBuilder<FirebaseToken> builder)
    {
        builder.HasKey(ft => ft.Id);

        builder.Property(ft => ft.Token).IsRequired();
        builder.Property(ft => ft.DataCadastro).IsRequired();

        builder.HasOne(ft => ft.User)
               .WithMany(u => u.FirebaseTokens)
               .HasForeignKey(ft => ft.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
