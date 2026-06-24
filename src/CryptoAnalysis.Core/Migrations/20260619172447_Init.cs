using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CryptoAnalysis.Core.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "asset",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Symbol = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CoinGeckoId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MarketCap = table.Column<decimal>(type: "numeric(28,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "candle",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssetId = table.Column<int>(type: "integer", nullable: false),
                    Ts = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Open = table.Column<decimal>(type: "numeric(28,8)", nullable: false),
                    High = table.Column<decimal>(type: "numeric(28,8)", nullable: false),
                    Low = table.Column<decimal>(type: "numeric(28,8)", nullable: false),
                    Close = table.Column<decimal>(type: "numeric(28,8)", nullable: false),
                    Volume = table.Column<decimal>(type: "numeric(28,8)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_candle_asset_AssetId",
                        column: x => x.AssetId,
                        principalTable: "asset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "metric",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssetId = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Period = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric", x => x.Id);
                    table.ForeignKey(
                        name: "FK_metric_asset_AssetId",
                        column: x => x.AssetId,
                        principalTable: "asset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_asset_Symbol",
                table: "asset",
                column: "Symbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_candle_AssetId_Ts",
                table: "candle",
                columns: new[] { "AssetId", "Ts" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_metric_AssetId_Type_Period",
                table: "metric",
                columns: new[] { "AssetId", "Type", "Period" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "candle");

            migrationBuilder.DropTable(
                name: "metric");

            migrationBuilder.DropTable(
                name: "asset");
        }
    }
}
