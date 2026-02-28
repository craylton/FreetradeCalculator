# FreetradeCalculator

A small command-line tool that reads a **Freetrade “activity feed export” CSV** and calculates your **realised profit/loss** per holding.

It’s intended as a quick way to answer questions like:
- "How much profit did I actually realise when I sold shares of X?"
- "What’s my cost basis for the shares I’ve sold so far?"
- "Did I make enough profit that I need to start loss-harvesting to offset it?"

The output is a simple console table showing (per instrument): total bought/sold, remaining quantity, realised P&L, sell proceeds, and cost basis — plus overall totals.

## Run it
Prerequisite: **.NET 10 SDK**.

From the repo root:

```bash
dotnet run --project FreetradeCalculator -- path/to/activity-feed-export.csv
```

## Run tests

```bash
dotnet test
```

## What this isn’t
This is not tax advice and it’s not a full portfolio tracker. It focuses on realised P&L based on the trade data in the export (and does not currently model every possible fee/tax field present in the CSV).