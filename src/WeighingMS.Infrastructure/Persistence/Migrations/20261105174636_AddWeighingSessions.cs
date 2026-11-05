using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeighingMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeighingSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "weighing_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_by = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    active_sequence = table.Column<int>(type: "integer", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weighing_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "weighing_session_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    chemical_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    suggested_container = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    scale_class = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    scale_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tolerance_source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    state = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    weighing_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scale_resolution = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    scale_unit = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    target_unit = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    target_value = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    tolerance_unit = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    tolerance_value = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weighing_session_components", x => x.id);
                    table.ForeignKey(
                        name: "fk_weighing_session_components_weighing_sessions_weighing_sess",
                        column: x => x.weighing_session_id,
                        principalTable: "weighing_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_weighing_session_components_weighing_session_id_sequence",
                table: "weighing_session_components",
                columns: new[] { "weighing_session_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_weighing_sessions_batch_id",
                table: "weighing_sessions",
                column: "batch_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "weighing_session_components");

            migrationBuilder.DropTable(
                name: "weighing_sessions");
        }
    }
}
