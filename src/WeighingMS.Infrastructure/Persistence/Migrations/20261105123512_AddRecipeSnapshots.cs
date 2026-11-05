using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeighingMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "recipe_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    order_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    machine = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    fabric = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    batch_weight_kg = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    shade = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    training_only = table.Column<bool>(type: "boolean", nullable: false),
                    loaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recipe_snapshot_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    chemical_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_colour = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    suggested_container = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    recipe_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_unit = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    target_value = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    tolerance_value = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_snapshot_components", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipe_snapshot_components_recipe_snapshots_recipe_snapshot",
                        column: x => x.recipe_snapshot_id,
                        principalTable: "recipe_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_recipe_snapshot_components_recipe_snapshot_id_sequence",
                table: "recipe_snapshot_components",
                columns: new[] { "recipe_snapshot_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recipe_snapshots_batch_id",
                table: "recipe_snapshots",
                column: "batch_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recipe_snapshot_components");

            migrationBuilder.DropTable(
                name: "recipe_snapshots");
        }
    }
}
