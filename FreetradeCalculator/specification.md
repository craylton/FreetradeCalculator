# Realised Profit Calculator — Specification

## 1. Overview

This application calculates **realised profit (or loss)** from a trading history CSV export (e.g., from Freetrade). It reads the CSV, extracts trade executions, matches sells against prior buys, and outputs a summary **per instrument/position** (by `Title`) showing realised profit.

The CSV file is the application input. The output is a list of positions where each position shows realised profit (or loss) from completed sells.

This tool calculates **realised** profit only. It does **not** compute unrealised P&L for any remaining (open) holdings.

## 2. Terms and assumptions

- **Instrument / Position**: A unique security identified by the CSV `Title` column.
- **Trade row**: A CSV row where `Type == ORDER`. Non-order rows (dividends, interest, deposits, etc.) are ignored.
- **Buy**: `Buy/Sell == BUY`.
- **Sell**: `Buy/Sell == SELL`.
- **Quantity (`Q`)**: Shares traded in the order.
- **Price per Share (`P`)**: `Price per Share in Account Currency`.
- **Account currency**: All monetary values are assumed already in the account currency.

Assumption (v1): no short-selling. If the history indicates selling more shares than currently held for a `Title`, the app must error.

## 3. CSV structure

### 3.1 Required columns

The application must read the following columns:

- `Type` (used to filter `ORDER`)
- `Buy/Sell` (BUY or SELL)
- `Title` (instrument key)
- `Price per Share in Account Currency` (decimal)
- `Quantity` (decimal; allow fractional shares)
- `Timestamp` (date/time; used for ordering)

Only rows with `Type == ORDER` are used.

### 3.2 Parsing rules

- Parse numeric fields using culture-invariant rules.
- Sort trade rows by `Timestamp` ascending before applying matching.
- Ignore unknown columns.
- Fail with a clear error if any required column is missing or unparseable.

## 4. Desired functionality

### 4.1 Inputs

- A CSV file containing trading history.

### 4.2 Outputs

Output a list of positions, one per `Title`, containing at least:

- `Title`
- Total quantity bought
- Total quantity sold
- Remaining quantity (open holding)
- Realised profit (or loss)

Optional fields (recommended for auditability):

- Total sell proceeds
- Total cost basis of sold shares
- First trade timestamp / last trade timestamp

### 4.3 Ordering and grouping

- Group trades by `Title`.
- Within each `Title` compute realised P&L using trades in ascending timestamp order.
- Output positions sorted by `Title` (alphabetical) unless configured otherwise.

## 5. Calculation method

### 5.1 Cost-basis method (v1): FIFO

Use **FIFO (First-In, First-Out)**. When selling shares, match the sold quantity against the oldest available buy lots.

This requires tracking buy lots (quantity and purchase price) per `Title`.

### 5.2 Equations

For a SELL order with:

- sell quantity: `Qs`
- sell price per share: `Ps`

Sell proceeds:

`Proceeds = Qs * Ps`

Cost basis is the sum of matched buy quantities multiplied by their buy prices. If the sell matches `n` buy lots where `qi` shares are taken from lot `i` purchased at `pbi`:

`CostBasis = ?(i=1..n) (qi * pbi)`

Realised profit for that sell:

`RP_sell = Proceeds - CostBasis`

Total realised profit for a position (`Title`):

`RP_position = ?(all sells for Title) RP_sell`

### 5.3 FIFO algorithm (per Title)

Maintain a FIFO queue of open buy lots.

Each buy lot stores:

- `quantityRemaining`
- `pricePerShare`
- `timestamp` (optional, for reporting)

Process each ORDER row in timestamp order:

1. If BUY: enqueue a new lot `(Q, P)`.
2. If SELL:
   - `remainingToSell = Qs`
   - `sellCostBasis = 0`
   - While `remainingToSell > 0`:
     - If no open lots exist: error (attempted short sale / oversell).
     - Take from the oldest lot: `consumed = min(remainingToSell, lot.quantityRemaining)`
     - Add to cost basis: `sellCostBasis += consumed * lot.pricePerShare`
     - Reduce lot quantity and `remainingToSell` accordingly
     - If a lot is depleted, dequeue it
   - `sellProceeds = Qs * Ps`
   - `realisedProfit += (sellProceeds - sellCostBasis)`

At the end:

- `realisedProfit` is realised P&L for the position.
- Remaining open lots represent the current holding quantity and its remaining cost basis.

## 6. Examples

### Example A: Basic partial sell

Trades:

- AAA BUY 10 @ £100
- BBB BUY 20 @ £15
- AAA SELL 5 @ £120

AAA (FIFO):

- Cost of sold shares = `5 * 100 = 500`
- Proceeds = `5 * 120 = 600`
- Realised profit = `600 - 500 = £100`

BBB:

- No sells ? realised profit = `£0`

### Example B: Sell spans multiple buys

Trades (AAA):

- BUY 10 @ £100
- BUY 10 @ £110
- SELL 15 @ £120

FIFO matching:

- 10 shares from £100 lot
- 5 shares from £110 lot

Calculations:

- Proceeds = `15 * 120 = 1800`
- Cost basis = `(10 * 100) + (5 * 110) = 1550`
- Realised profit = `1800 - 1550 = £250`
- Remaining holding: 5 @ £110

### Example C: Multiple sells, including a loss

Trades (AAA):

- BUY 10 @ £100
- SELL 4 @ £90
- SELL 6 @ £130

Sell 1:

- Proceeds = `4 * 90 = 360`
- Cost basis = `4 * 100 = 400`
- Realised profit = `-£40`

Sell 2:

- Proceeds = `6 * 130 = 780`
- Cost basis = `6 * 100 = 600`
- Realised profit = `£180`

Total realised profit = `-40 + 180 = £140`

### Example D: Oversell (error)

Trades (AAA):

- BUY 5 @ £100
- SELL 6 @ £110

This must error because it attempts to sell more shares than are currently held.

## 7. Validation and error handling

The app must validate:

- CSV file exists and is readable.
- Required headers exist.
- Each ORDER row has a valid timestamp, title, side (BUY/SELL), quantity > 0, and price >= 0.
- Sells cannot exceed available remaining buy quantity (no shorting in v1).

Error messages should identify the instrument (`Title`) and the offending row (timestamp and/or CSV line number if available).

## 8. Non-goals (v1)

- FX conversion between currencies (assume account-currency prices are correct).
- Dividends, interest, deposits/withdrawals.
- Corporate actions (splits/mergers) unless represented as ORDER rows in an unambiguous way.
- Alternate cost basis methods (e.g., average cost, LIFO).

## 9. Acceptance criteria

- Given a valid CSV, the app outputs one summary per `Title`.
- Realised profit matches FIFO calculations as demonstrated in the examples.
- Non-ORDER rows do not affect realised P&L.
- Overselling results in a clear error.
- Output is deterministic (stable timestamp ordering + consistent formatting).
