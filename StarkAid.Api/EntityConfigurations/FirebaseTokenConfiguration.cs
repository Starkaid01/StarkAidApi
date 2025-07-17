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
            builder.Property(x => x.Token).IsRequired();
            builder.Property(x => x.DataCadastro).IsRequired();
            builder.HasOne<User>() // ou o nome do seu model User
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
