using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeighingMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumptionWriteBack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orgatex_write_backs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lines = table.Column<string>(type: "jsonb", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orgatex_write_backs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_orgatex_write_backs_batch_id",
                table: "orgatex_write_backs",
                column: "batch_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orgatex_write_backs_idempotency_key",
                table: "orgatex_write_backs",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orgatex_write_backs_status_next_attempt_at",
                table: "orgatex_write_backs",
                columns: new[] { "status", "next_attempt_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "orgatex_write_backs");
        }
    }
}
