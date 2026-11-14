# 0003. Reserve refund amounts before calling the bank

- Status: accepted
- Date: 2026-11-14

## Context

A refund involves an external bank call that can fail or time out. If the refundable amount were checked and only updated after the bank responded, two concurrent refund requests could both pass the check and the bank would refund more than was captured.

## Decision

- `Payment.RequestRefund` creates a `Refund` in `Pending` state before any bank call. Pending refunds count against `RefundableAmount`.
- The bank call happens after the reservation is persisted. Its result completes the refund (`Succeeded`) or releases the reservation (`Failed`).
- The payment status changes only when a refund succeeds: `PartiallyRefunded` while some amount remains, `Refunded` when the succeeded total equals the captured amount.
- Only succeeded refunds produce ledger entries.

## Consequences

- The domain guarantees the sum of pending and succeeded refunds never exceeds the captured amount. Concurrent requests are serialized by the payment row's optimistic concurrency token, which arrives with persistence.
- A refund stuck in `Pending` (for example after a bank timeout) keeps its amount reserved until it is reconciled, which is the safe side for money leaving the system.
