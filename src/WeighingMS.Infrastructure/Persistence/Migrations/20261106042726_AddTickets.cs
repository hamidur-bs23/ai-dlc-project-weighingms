using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeighingMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tickets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    barcode_value = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    print_count = table.Column<int>(type: "integer", nullable: false),
                    first_print_key = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    first_printed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_printed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lines = table.Column<string>(type: "jsonb", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tickets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "print_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_reprint = table.Column<bool>(type: "boolean", nullable: false),
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: false),
                    operator_badge_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    attempted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_print_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_print_jobs_tickets_ticket_id",
                        column: x => x.ticket_id,
                        principalTable: "tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_print_jobs_ticket_id",
                table: "print_jobs",
                column: "ticket_id");

            migrationBuilder.CreateIndex(
                name: "ix_tickets_batch_id",
                table: "tickets",
                column: "batch_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "print_jobs");

            migrationBuilder.DropTable(
                name: "tickets");
        }
    }
}
