using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigrate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "InvitationLinkSnapshots",
                schema: "identity",
                columns: table => new
                {
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Aggregate = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitationLinkSnapshots", x => new { x.Version, x.AggregateId });
                });

            migrationBuilder.CreateTable(
                name: "InvitationLinkStoreEvents",
                schema: "identity",
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
                    table.PrimaryKey("PK_InvitationLinkStoreEvents", x => new { x.Version, x.AggregateId });
                });

            migrationBuilder.CreateTable(
                name: "UserSnapshots",
                schema: "identity",
                columns: table => new
                {
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Aggregate = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSnapshots", x => new { x.Version, x.AggregateId });
                });

            migrationBuilder.CreateTable(
                name: "UserStoreEvents",
                schema: "identity",
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
                    table.PrimaryKey("PK_UserStoreEvents", x => new { x.Version, x.AggregateId });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvitationLinkSnapshots",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "InvitationLinkStoreEvents",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserSnapshots",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserStoreEvents",
                schema: "identity");
        }
    }
}
