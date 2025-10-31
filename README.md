🏗️ High-Level Architecture

I structured the solution using Clean Architecture, separating concerns into distinct layers:

- **Api Layer**: Handles HTTP endpoints, validation, and request routing.
- **Application Layer**: Encapsulates domain logic, MediatR commands, and strategies (e.g., balance adjustment).
- **Infrastructure Layer**: Implements persistence, ECB integration, jobs, caching, and rate limiting.

All dependencies flow inward — lower layers have no references to upper layers, ensuring testability and maintainability.

---

⚙️ Core Implementations

### 1️⃣ ECB Gateway Library

- Created a dedicated library (`EcbRateService`) that acts as a gateway between the app and ECB API.
- Uses `HttpClient` abstraction (`IHttpClientHelper`) for request resiliency.
- Parses ECB XML data into strongly-typed `CurrencyRate` entities.
- Updates the DB via a PostgreSQL UPSERT (`ON CONFLICT DO UPDATE`) for high efficiency.
- Results are cached in-memory (`IMemoryCache`) for read performance.

✅ **Benefit**: Separation of concerns, reusable across other services, and faster read operations.

---

### 2️⃣ Periodic Background Job

- Integrated **Quartz.NET** to run `EcbRateUpdateJob` every minute.
- Fetches, parses, and stores ECB currency rates.
- Supports retry/backoff (via Polly) and misfire handling to ensure reliability under network or DB transient errors.

✅ **Benefit**: High reliability and automatic synchronization of currency data.

---

### 3️⃣ Wallet Service

- Implements wallet operations (create, get, adjust) with concurrency-safe updates using `SemaphoreSlim`.
- Uses **Strategy Pattern** to encapsulate balance operations (`AddFunds`, `SubtractFunds`, etc.).
- Employs caching and invalidation to optimize repeated lookups.

✅ **Benefit**: Safe concurrent updates and easily extendable strategies for new business rules.

---

### 4️⃣ Caching & Performance

- Rates and wallets are cached in-memory.
- Cache invalidation triggers after balance changes.
- ECB rate cache automatically refreshed after each job run.

✅ **Benefit**: Reduces DB load and ensures sub-millisecond reads for frequent queries.

---

### 5️⃣ Rate Limiting

- Configured per-client IP **Token Bucket Rate Limiter**.
- Allows 30 requests per minute per client.

✅ **Benefit**: Prevents abuse and ensures fair access to APIs.

---

🧩 Patterns Used

| Pattern                          | Purpose                                              | Benefit                               |
|----------------------------------|------------------------------------------------------|---------------------------------------|
| Strategy Pattern                 | Handle different wallet adjustment algorithms dynamically | Extensible, no if/else clutter        |
| Factory Pattern                  | Create wallet strategies dynamically by name         | Promotes loose coupling               |
| Dependency Injection (Autofac)   | Manage lifetimes and dependencies                    | High testability                      |
| Caching                          | Store frequently accessed data                       | Boosts performance                    |
| Rate Limiting                    | Control request bursts                               | Prevents service degradation          |