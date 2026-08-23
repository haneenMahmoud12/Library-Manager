# My Library Manager Requirements

## MVP functional requirements

### Account
 - User can register.
 - User confirms email.
 - User can log in.
 - User can refresh authentication.
 - User can log out.

### Library
 - A user has at least one personal library.
 - User can create a library.
 - User can view libraries they belong to.
 - User can rename a library.
 - User can optionally specify its visibility later.

### Catalogue
 - System stores books.
 - Books may have one or more authors.
 - A book may have multiple editions.
 - An edition may have ISBN-10 and/or ISBN-13.
 - An edition may have a publisher.
 - Metadata may come from an external provider.

### Book copies
 - User can add a specific edition to a library.
 - User can own multiple copies of the same edition.
 - A copy can have:
    - format
    - reading status
    - optional location
    - notes

### ISBN lookup
 - User can submit an ISBN.
 - System checks existing catalogue first.
 - If not found, it queries an external book API.
 - Metadata is normalized into your domain model.
 - User confirms before adding.

### Duplicate detection
 - Before adding a copy, system should tell the user:

    - exact edition already owned;
    - another edition of the same book is owned;
    - book is not currently owned.

### Library browsing
 - List books.
 - Search by title.
 - Search by author.
 - Search by ISBN.
 - Filter by format or reading status.

## Later requirements

 - Bulk scanning
 - Redis
 - Background imports
 - Loans
 - Wishlist
 - Friends
 - Shared family libraries
 - OCR
 - Offline mode
 - Notifications