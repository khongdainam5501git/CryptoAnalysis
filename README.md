# CryptoAnalysis

A small system that downloads crypto prices (BTC and ETH), saves them in a database, analyzes them, and creates a PDF report.

Built with ASP.NET Core 10, EF Core 10, PostgreSQL, and the CoinGecko API. It runs locally in VS Code. No Docker.

## What it does

1. Downloads price data (OHLCV) for BTC and ETH from CoinGecko.
2. Saves the data in PostgreSQL (skips duplicates).
3. Calculates statistics, volatility, and the Pearson correlation between BTC and ETH.
4. Builds a PDF report with a table, the correlation, and a price chart.

## Requirements

- .NET 10 SDK (check with `dotnet --version`)
- PostgreSQL: `brew install postgresql@16 && brew services start postgresql@16`

## Run it

```bash
# 1. Create the database (only once)
createdb cryptoanalysis

# 2. Start the app (it creates the tables automatically)
dotnet run --project src/CryptoAnalysis.Api
```

Then open Swagger: <http://localhost:5099/swagger>

## How to use

Call the endpoints in this order:

1. `POST /ingest` — download BTC and ETH data into the database.
2. `POST /reports` — analyze the data and download the PDF report.

You can call them in Swagger, with the `requests.http` file, or with curl:

```bash
# Download data
curl -X POST http://localhost:5099/ingest \
  -H "Content-Type: application/json" \
  -d '{ "assets": ["BTC", "ETH"], "days": 180 }'

# Get the PDF report
curl -X POST http://localhost:5099/reports \
  -H "Content-Type: application/json" \
  -d '{ "assets": ["BTC", "ETH"] }' \
  --output report.pdf
```

## Endpoints

| Method | Route | What it does |
|---|---|---|
| `POST` | `/ingest` | Download BTC/ETH data into the database |
| `GET`  | `/assets` | List assets and how many candles each has |
| `GET`  | `/candles?symbol=BTC&limit=10` | View the raw price data |
| `POST` | `/reports` | Analyze and create the PDF report |

## Look at the database

```bash
# How many candles per coin
psql cryptoanalysis -c 'SELECT a."Symbol", count(*) FROM candle c JOIN asset a ON a."Id"=c."AssetId" GROUP BY a."Symbol";'

# The analysis results
psql cryptoanalysis -c 'SELECT "Type","Value" FROM metric ORDER BY "ComputedAt" DESC;'
```

## Project layout

```
src/CryptoAnalysis.Api/     Web API, controllers, Swagger
src/CryptoAnalysis.Core/    Database, data download, analysis, charts, reports
tests/CryptoAnalysis.Tests/ Unit tests
docs/                       Diagrams and the report
```

## Tests

```bash
dotnet test
```

## Settings

- Database connection: `src/CryptoAnalysis.Api/appsettings.json` (`ConnectionStrings:Postgres`). Change `Username` to match your PostgreSQL user.
- CoinGecko API key: `appsettings.Development.json` (`CoinGecko:ApiKey`). A demo key is already set.

## Note about the data

On the free CoinGecko plan, `days=180` returns about 45 candles per coin (one candle every 4 days), not 180 daily candles. This is real data and is enough for the analysis.

## If something breaks

- **Database connection fails**: check `brew services list`, then make sure `Username` in `appsettings.json` matches your PostgreSQL user (`psql -l` shows your databases).
- **`/reports` says no data**: run `POST /ingest` first.
