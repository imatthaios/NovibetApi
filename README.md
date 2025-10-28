# NovibetApi

## Architecture Overview
| Layer                | Responsibility                                                                                          |
| -------------------- | ------------------------------------------------------------------------------------------------------- |
| **API**              | Only receives HTTP requests, passes them to MediatR or services, returns responses. No business logic.  |
| **Application**      | Core business layer. Defines commands, queries, handlers, and **services** implementing business rules. |
| **Domain**           | Entities and value objects only (no EF, no logic).                                                      |
| **Infrastructure**   | Data access (EF Core, external services, etc.).                                                         |
| **MediatR Handlers** | Coordinate request flow — **delegate to domain/application services**.                                  |
| **Services**         | Implement actual logic (validation, db ops, strategy selection).                                        |

