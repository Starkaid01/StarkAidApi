using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class EnsureEconomyColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotente: só adiciona se não existir
            migrationBuilder.Sql(@"
IF COL_LENGTH('Users','PlanType') IS NULL
    ALTER TABLE Users ADD PlanType INT NOT NULL CONSTRAINT DF_Users_PlanType DEFAULT(0);
IF COL_LENGTH('Users','StarkCoins') IS NULL
    ALTER TABLE Users ADD StarkCoins INT NOT NULL CONSTRAINT DF_Users_StarkCoins DEFAULT(0);
IF COL_LENGTH('Users','TokensConsumidosSemana') IS NULL
    ALTER TABLE Users ADD TokensConsumidosSemana INT NOT NULL CONSTRAINT DF_Users_TokensConsumidosSemana DEFAULT(0);

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StarkCoinPurchases' AND TABLE_SCHEMA = 'dbo')
BEGIN
    CREATE TABLE [dbo].[StarkCoinPurchases](
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [PackageType] INT NOT NULL,
        [StarkCoinsAmount] INT NOT NULL,
        [Price] DECIMAL(18,2) NOT NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_StarkCoinPurchases_Users_UserId FOREIGN KEY([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX IX_StarkCoinPurchases_UserId ON [dbo].[StarkCoinPurchases]([UserId]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Não remover colunas/tabela em Down para evitar perda de dados
        }
    }
}
