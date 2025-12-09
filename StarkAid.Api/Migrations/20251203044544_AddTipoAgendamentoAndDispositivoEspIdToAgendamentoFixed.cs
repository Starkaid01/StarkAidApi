using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoAgendamentoAndDispositivoEspIdToAgendamentoFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove foreign key se existir
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Agendamentos_Devices_DeviceId1')
                BEGIN
                    ALTER TABLE [Agendamentos] DROP CONSTRAINT [FK_Agendamentos_Devices_DeviceId1];
                END
            ");

            // Remove index se existir
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Agendamentos_DeviceId1' AND object_id = OBJECT_ID('Agendamentos'))
                BEGIN
                    DROP INDEX [IX_Agendamentos_DeviceId1] ON [Agendamentos];
                END
            ");

            // Remove coluna se existir
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Agendamentos') AND name = 'DeviceId1')
                BEGIN
                    ALTER TABLE [Agendamentos] DROP COLUMN [DeviceId1];
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
