using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarkAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoAgendamentoAndDispositivoEspIdToAgendamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agendamentos_Devices_DeviceId",
                table: "Agendamentos");

            migrationBuilder.AlterColumn<Guid>(
                name: "DeviceId",
                table: "Agendamentos",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            // DispositivoEspId já existe no banco - não adicionar novamente
            // migrationBuilder.AddColumn<Guid>(
            //     name: "DispositivoEspId",
            //     table: "Agendamentos",
            //     type: "uniqueidentifier",
            //     nullable: true);

            // TipoAgendamento já existe no banco - não adicionar novamente
            // migrationBuilder.AddColumn<int>(
            //     name: "TipoAgendamento",
            //     table: "Agendamentos",
            //     type: "int",
            //     nullable: false,
            //     defaultValue: 1);

            // Index já existe no banco - não criar novamente
            // migrationBuilder.CreateIndex(
            //     name: "IX_Agendamentos_DispositivoEspId",
            //     table: "Agendamentos",
            //     column: "DispositivoEspId");

            migrationBuilder.AddForeignKey(
                name: "FK_Agendamentos_Devices_DeviceId",
                table: "Agendamentos",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Foreign key já existe no banco - não criar novamente
            // migrationBuilder.AddForeignKey(
            //     name: "FK_Agendamentos_DispositivosEsp_DispositivoEspId",
            //     table: "Agendamentos",
            //     column: "DispositivoEspId",
            //     principalTable: "DispositivosEsp",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agendamentos_Devices_DeviceId",
                table: "Agendamentos");

            migrationBuilder.DropForeignKey(
                name: "FK_Agendamentos_DispositivosEsp_DispositivoEspId",
                table: "Agendamentos");

            migrationBuilder.DropIndex(
                name: "IX_Agendamentos_DispositivoEspId",
                table: "Agendamentos");

            migrationBuilder.DropColumn(
                name: "DispositivoEspId",
                table: "Agendamentos");

            migrationBuilder.DropColumn(
                name: "TipoAgendamento",
                table: "Agendamentos");

            migrationBuilder.AlterColumn<Guid>(
                name: "DeviceId",
                table: "Agendamentos",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Agendamentos_Devices_DeviceId",
                table: "Agendamentos",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
