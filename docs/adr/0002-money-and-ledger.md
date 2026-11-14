# 0002. Money as integer minor units and a double-entry ledger

- Status: accepted
- Date: 2026-11-14

## Context

Floating-point types cannot represent most decimal fractions exactly, so `0.1 + 0.2` style errors accumulate across many payments. `decimal` is exact but invites accidental fractional kuruş values and needs care at every serialization boundary. Balances also need an audit trail that explains every change, not a single mutable number.

## Decision

- `Money` stores the amount as a `long` in minor units (kuruş, cent) together with an ISO 4217 currency. `150,75 TRY` is `15075 TRY`.
- Supported currencies are TRY, USD and EUR, all with two minor digits. There is no currency conversion.
- Arithmetic between different currencies throws. Addition checks for overflow instead of wrapping around. Negative amounts cannot be created.
- Each merchant has two ledger accounts. Every journal line carries its currency, so balances are always reported per currency:
  - `BankSettlement`: money the acquiring bank owes us for the merchant's payments (debit-normal).
  - `MerchantBalance`: money we owe the merchant (credit-normal).
- A captured payment posts debit `BankSettlement`, credit `MerchantBalance`. A succeeded refund posts the reverse for the refunded amount.
- Authorization, cancellation and failed attempts post nothing, because no money moves.
- A `JournalEntry` cannot be created unless debits equal credits, all lines share one currency and merchant, and every line is positive. Entries are immutable; corrections are new entries.

## Consequences

- Rounding cannot occur inside the system; formatting to `150,75 ₺` happens only at the presentation edge.
- A merchant's balance is the sum of its ledger lines, so it can always be recomputed and explained line by line.
- Fees, payouts and multi-currency settlement are not modelled. Adding fees later means adding a revenue account and a third line to the capture entry.
