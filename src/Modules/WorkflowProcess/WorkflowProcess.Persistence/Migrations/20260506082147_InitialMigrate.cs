using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkflowProcess.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigrate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "workflow_process");

            migrationBuilder.CreateTable(
                name: "WorkflowProcessSnapshots",
                schema: "workflow_process",
                columns: table => new
                {
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Aggregate = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowProcessSnapshots", x => new { x.Version, x.AggregateId });
                });

            migrationBuilder.CreateTable(
                name: "WorkflowProcessStoreEvents",
                schema: "workflow_process",
                columns: table => new
                {
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    Event = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowProcessStoreEvents", x => new { x.Version, x.AggregateId });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowProcessSnapshots",
                schema: "workflow_process");

            migrationBuilder.DropTable(
                name: "WorkflowProcessStoreEvents",
                schema: "workflow_process");
        }
    }
}
