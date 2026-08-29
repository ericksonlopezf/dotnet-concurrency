# Idempotency vs Concurrency

## 1. Responsibility Separation

```mermaid
graph TD
    subgraph Request Pipeline
        Req[Incoming Request] --> IdemCheck{EricksonLopez.Idempotency<br/>Is duplicate request?}
        IdemCheck -- Yes --> Cached[Return Cached Result]
        IdemCheck -- No --> ConcCheck{EricksonLopez.Concurrency<br/>Is state version fresh?}
        ConcCheck -- Stale --> ConflictErr[Return Result.Conflict]
        ConcCheck -- Fresh --> Trans[EricksonLopez.Transactions<br/>Execute atomic Unit of Work]
    end
```

---

## 2. Distinction Summary

- **Idempotency**: *"Is this the same request repeated?"* (Protects against network retries of identical operations).
- **Concurrency**: *"Is this operation modifying a state that has changed since it was read?"* (Protects against race conditions and stale writes).
