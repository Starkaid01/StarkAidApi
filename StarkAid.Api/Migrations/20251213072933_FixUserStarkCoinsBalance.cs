using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixUserStarkCoinsBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Creditar 100 StarkCoins e resetar tokens para usuário de teste
            migrationBuilder.Sql(@"
                UPDATE Users 
                SET StarkCoinBalance = 100,
                    TokensConsumidosSemana = 0
                WHERE Id = '5468b574-4895-404c-1961-08de2312a7c2'
                    AND StarkCoinBalance = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
