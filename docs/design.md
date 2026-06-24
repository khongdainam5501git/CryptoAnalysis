# Design — CryptoAnalysis

Crypto market analysis system using open exchange data (Task #2).

This document covers the architecture, data model, and ETL. The diagrams are Mermaid files in this `docs/` folder (`architecture.mermaid`, `er-model.mermaid`, `etl.mermaid`); view them in VS Code or on mermaid.live.

## 1. Architecture — 6 logical modules

The code is split into 2 projects (`Api`, `Core`), but described as **6 logical modules** to match the task:

| # | Module | Responsibility | Code |
|---|---|---|---|
| 1 | Web API + Swagger | HTTP entry point, API docs, triggers ETL and reports | `IngestionController`, `ReportsController`, `CandlesController` |
| 2 | Ingestion | Call CoinGecko, parse, load into DB without duplicates | `CoinGeckoClient`, `IngestionService` |
| 3 | Data | Relational storage via EF Core | `CryptoDbContext`, `Asset/Candle/Metric` |
| 4 | Analytics | Stats, volatility, Pearson | `Returns`, `AnalysisService` |
| 5 | Charts | Price line chart -> PNG | `PriceChartService` (ScottPlot) |
| 6 | Reports | Build PDF with data and charts | `PdfReportService` (QuestPDF) |

Data flow: `CoinGecko -> Ingestion -> Data (PostgreSQL) -> Analytics -> Charts/Reports -> PDF`.

## 2. Data model — 3 tables

- **asset** — list of assets (BTC, ETH). `Symbol` is unique; `CoinGeckoId` is used to call the API (`bitcoin`, `ethereum`).
- **candle** — daily OHLCV candles. A **unique (AssetId, Ts)** constraint prevents duplicates; an index on `(AssetId, Ts)` speeds up time-series queries.
- **metric** — analysis results (volatility per coin, correlation per pair). `AssetId` is nullable for pair-level values (BTC-ETH Pearson).

## 3. ETL pipeline

- **Extract** — `CoinGeckoClient` calls `GET /coins/{id}/ohlc?vs_currency=usd&days=180` for `[ts, open, high, low, close]`, and `GET /coins/{id}/market_chart?...&interval=daily` for `total_volumes`.
- **Transform** — parse JSON; match volume by day (truncate timestamp to UTC day); map to the `Candle` entity and set `AssetId`.
- **Load** — `IngestionService` filters out rows that already exist by `(AssetId, Ts)`, then `AddRange + SaveChanges`. Seeded once by hand (no cron).

### Note on data limits (important for the report)

The CoinGecko free/demo tier picks the OHLC granularity automatically: 30-minute candles for 1-2 days, 4-hour candles for 3-30 days, 4-day candles for 31+ days. So with `days=180`, the `/ohlc` endpoint returns about 45 candles (one every 4 days), not 180 daily candles. This is real OHLC data and is enough for returns, volatility, and Pearson. Volume comes separately from `/market_chart` (daily) and is matched by nearest day. This is a limit of the open data source to keep in mind when reading the results.
