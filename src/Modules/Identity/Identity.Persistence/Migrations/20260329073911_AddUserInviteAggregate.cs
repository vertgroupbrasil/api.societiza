using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserInviteAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserInviteSnapshots",
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
                    table.PrimaryKey("PK_UserInviteSnapshots", x => new { x.Version, x.AggregateId });
                });

            migrationBuilder.CreateTable(
                name: "UserInviteStoreEvents",
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
                    table.PrimaryKey("PK_UserInviteStoreEvents", x => new { x.Version, x.AggregateId });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserInviteSnapshots",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserInviteStoreEvents",
                schema: "identity");
        }
    }
}
