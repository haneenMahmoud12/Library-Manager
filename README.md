# 📚 Personal Library Manager

A mobile-first personal library management application built with **.NET** that helps users catalogue and manage their physical book collections with minimal manual data entry.

The project was inspired by a simple problem: **forgetting whether you already own a book and accidentally buying it again.** This problem is faced by many readers who own a huge amount of books (100+ books).

Instead of manually entering hundreds of books, users can scan a book's ISBN barcode to automatically identify it, retrieve its metadata, check for duplicates, and add it to their library.

> 🚧 **Status:** Under active development

---

## ✨ Key Features

### 📷 ISBN Barcode Scanning

Scan the barcode on a physical book to automatically retrieve information such as:

* Title
* Author
* ISBN
* Publisher
* Publication date
* Language
* Cover

```text
Scan Barcode
     ↓
Extract ISBN
     ↓
Check Existing Library
     ↓
Retrieve Book Metadata
     ↓
Confirm
     ↓
Add to Library
```

### 🚨 Duplicate Detection

Before adding or purchasing a book, users can quickly check whether they:

* Own the same edition
* Own another edition of the same title
* Do not currently own the book

### ⚡ Bulk Scanning

Designed for users with large existing collections.

Bulk Scan Mode allows users to continuously scan books without confirming every book individually.

```text
✓ The Picture of Dorian Gray
✓ Crime and Punishment
⚠ 1984 — Already in library
✓ The Hobbit
```

The resulting import batch can then be reviewed and processed together.

### 🔍 Search & Organisation

Users can:

* Search by title, author or ISBN
* Filter their collection
* Track reading status
* Record the physical location of a book
* Maintain wishlists
* Browse different editions

### 👨‍👩‍👧 Shared Libraries

Support for personal and shared collections, allowing households to maintain a common library while controlling access through roles such as:

* Owner
* Editor
* Viewer

### 🤝 Lending

Track books lent to friends or family, including borrower and return information.

---

## 🛠️ Tech Stack

### Backend

* **C# / .NET**
* **ASP.NET Core Web API**
* **Entity Framework Core**
* **SQL**

### Mobile

* **.NET MAUI**
* **SQLite** for local/offline data

### Infrastructure

* **Redis** — distributed caching
* **Docker / Docker Compose**
* **GitHub Actions** — CI/CD

### Testing

* **xUnit**
* Unit tests
* Integration tests

### Integrations

* ISBN barcode scanning
* External book metadata APIs
* OCR planned as a fallback for books without readable barcodes

---

## 🏗️ Architecture

The backend is designed as a **modular monolith**, keeping the system simple to deploy while maintaining clear domain boundaries.

```text
                    ┌─────────────────┐
                    │   .NET MAUI     │
                    │   Mobile App    │
                    └────────┬────────┘
                             │
                             │ REST
                             ▼
                    ┌─────────────────┐
                    │ ASP.NET Core API│
                    └────────┬────────┘
                             │
           ┌─────────────────┼─────────────────┐
           │                 │                 │
           ▼                 ▼                 ▼
       Identity          Catalogue         Libraries
                                             │
                             ┌───────────────┼───────────────┐
                             ▼               ▼               ▼
                          Imports          Loans          Wishlist

                             │
                   ┌─────────┴──────────┐
                   ▼                    ▼
               PostgreSQL            Redis
```

Initial domain modules include:

* **Identity** — authentication and users
* **Catalogue** — books, editions, authors and metadata
* **Libraries** — collections, memberships and book locations
* **Imports** — barcode and bulk import processing
* **Loans** — lending and returns

---

## 🔌 Book Metadata Integration

Book metadata is retrieved using external providers behind a provider abstraction.

```text
ISBN
 │
 ▼
Redis Cache
 │
 ├── Hit ─────────────────────► Result
 │
 └── Miss
       │
       ▼
Primary Metadata Provider
       │
       ├── Found ──────────────► Cache → Result
       │
       └── Not Found
              │
              ▼
       Fallback Provider
```

This allows metadata providers to be replaced or combined without coupling the core application to a particular external API.

---

## 🧠 Engineering Concepts

The project is designed to demonstrate practical backend engineering concepts including:

* REST API design
* Modular architecture
* Domain modelling
* SOLID principles
* Dependency Injection
* Authentication & authorization
* JWT & refresh tokens
* Entity Framework Core
* Relational database design
* Database indexing
* Optimistic concurrency
* External API integration
* `HttpClientFactory`
* Provider/Adapter Pattern
* Resilience and fallback strategies
* Redis caching
* Cache-Aside Pattern
* Background processing
* Bulk operations
* Offline synchronisation
* Docker
* CI/CD
* Unit & integration testing

---

## 📖 Book vs Edition

The application distinguishes between a **book/work** and a specific **edition**.

```text
The Hobbit
│
├── English Edition
│   ├── ISBN: ...
│   └── Publisher: ...
│
├── Another English Edition
│   ├── ISBN: ...
│   └── Publisher: ...
│
└── Arabic Edition
    ├── ISBN: ...
    └── Publisher: ...
```

This allows duplicate detection to distinguish between:

> ⚠️ You already own this exact edition.

and:

> ℹ️ You own another edition of this book.

---

## 🗺️ Roadmap

### Phase 1 — Core Library

* [ ] Project architecture
* [ ] Authentication
* [ ] User libraries
* [ ] Books, editions and authors
* [ ] Library locations
* [ ] Search and filtering

### Phase 2 — Smart Scanning

* [ ] ISBN barcode scanning
* [ ] Automatic metadata lookup
* [ ] Duplicate detection
* [ ] Provider fallback
* [ ] Bulk scanning

### Phase 3 — Production Features

* [ ] Redis caching
* [ ] Background import processing
* [ ] Shared libraries
* [ ] Lending
* [ ] Wishlist
* [ ] Offline support

### Phase 4 — Delivery

* [ ] Unit tests
* [ ] Integration tests
* [ ] Docker
* [ ] CI/CD
* [ ] Production deployment
* [ ] Logging and health checks

### Future

* [ ] OCR fallback
* [ ] Bookshelf photo recognition
* [ ] Reading statistics
* [ ] Recommendations
* [ ] Notifications

---

## 📂 Repository Structure

```text
PersonalLibraryManager/
├── src/
│   ├── Library.Api/
│   ├── Library.Application/
│   ├── Library.Domain/
│   ├── Library.Infrastructure/
│   └── Library.Mobile/
│
├── tests/
│   ├── Library.UnitTests/
│   └── Library.IntegrationTests/
│
├── docs/
│   ├── Architecture.md
│   ├── DatabaseDesign.md
│   └── ADR/
│
├── .github/
│   └── workflows/
│
├── docker-compose.yml
└── README.md
```

---

## 🎯 Project Goals

This project aims to:

1. **Solve a real problem** for people managing large physical book collections.
2. **Minimise manual data entry** through barcode scanning and automatic metadata retrieval.
3. **Demonstrate production-oriented .NET backend engineering** through architecture, integrations, caching, background processing, testing and deployment.
4. Deliver a complete application that can be used as both a real product and a software engineering portfolio project.

---

## 🚧 Current Status

The project is currently under active development.

**Current focus:** Phase 1 — Core Library
