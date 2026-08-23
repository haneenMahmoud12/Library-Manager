# Personal Library Manager Milestones

## Milestone 0 — Foundation

- Solution structure
- Domain, Application, Infrastructure, and API projects
- SQL Server
- EF Core
- Initial entities
- Identity
  - Register
  - Confirm email
  - Login
  - Refresh token

## Milestone 1 — Core Library

- Create library
- Get my libraries
- Update library
- Establish library membership foundation
- Add `Book`
- Add `Author`
- Add `Publisher`
- Add `BookEdition`
- Add `BookCopy`
- Add a book manually
- Get library contents

### Goal
An authenticated user can create a library and manually add books to it.

## Milestone 2 — Smart ISBN Workflow

- ISBN validation
- `GET /books/lookup/{isbn}`
- Open Library integration
- Metadata mapping
- Provider abstraction
- Duplicate detection
- Add scanned edition to library

### Goal
Given an ISBN, the app can identify the book and tell the user whether they already own it.

## Milestone 3 — Mobile MVP

### Screens
- Login
- Register
- Home
- My Library
- Scan Book
- Book Lookup Result
- Book Details
- Add Book

### Interaction

```text
Scan ISBN
    ↓
API lookup
    ↓
"You already own this"
        OR
"Add to Library"
```

## Milestone 4 — Search & Organization

- Search
- Filters
- Reading status
- Library locations
- Edit copy details
- Remove copy
- Pagination

## Milestone 5 — Bulk Collection Import

- Bulk scan mode
- Import batch
- Import items
- Background processing
- Duplicate summary
- Failed lookup review

## Milestone 6 — Production Engineering

- Redis
- Cache-aside
- Resilient external API calls
- Retries and timeouts
- Structured logging
- Health checks
- Unit tests
- Integration tests
- Docker
- CI/CD
- Production deployment

## Milestone 7 — Family & Social Features

- Shared libraries
- Owner / Editor / Viewer roles
- Invitations
- Loans
- Wishlist
- Friends
- Friend library visibility