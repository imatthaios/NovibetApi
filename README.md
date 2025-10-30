## 📌 Project: Novibet Wallet API

📚 Architecture: Clean Architecture + CQRS + Strategy Pattern + Background Job (Quartz) + PostgreSQL

🧱 Technologies: ASP.NET Core 8, MediatR, Autofac, Entity Framework Core, Quartz.NET, MemoryCache

☁️ Integration: ECB Exchange Rates (XML feed)

### Architecture Layers
| Layer              | Responsibility                                        | Key Tech                       |
| ------------------ | ----------------------------------------------------- | ------------------------------ |
| **Domain**         | Core business entities (`Wallet`, `CurrencyRate`)     | Plain C# models                |
| **Application**    | CQRS commands, handlers, MediatR requests, strategies | MediatR, Strategy Pattern      |
| **Infrastructure** | EF Core context, ECB integration, background jobs     | PostgreSQL, Quartz, HttpClient |
| **API**            | Controllers, endpoints, DI container setup            | ASP.NET Core 8, Autofac        |

### Assessment
