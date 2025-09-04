using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarkAid.Api.Entities;

namespace StarkAid.Api.EntityConfigurations
{
    public class FirebaseTokenConfiguration : IEntityTypeConfiguration<FirebaseToken>
    {
        public void Configure(EntityTypeBuilder<FirebaseToken> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Token)
                .IsRequired();

            builder.Property(x => x.DataCadastro)
                .IsRequired();

            builder.HasOne(x => x.User)
                .WithMany(u => u.FirebaseTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}