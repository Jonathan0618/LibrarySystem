# Student Dashboard — What Belongs Beyond the Catalog

A realistic student-facing dashboard in a school library system typically covers the following areas, in addition to catalog browsing/search.

## Their own loans

- What's currently checked out, due dates, and renewals remaining
- A one-click "renew" action, with server-side rules enforced (e.g. no renewal if another member has a reservation on that title)
- Overdue status and what happens next (fine accrual, hold on further checkouts)

## Reservations / holds

- Placing a hold on a book that's currently checked out
- Seeing queue position
- Notification when a hold is ready for pickup, and how long they have to collect it before it expires
- Cancelling a hold they no longer want

Typical reservation lifecycle: **Waiting → Ready for Pickup → Fulfilled / Expired / Cancelled**

## Fines and obligations *(if the school enables fines)*

- Current balance owed
- What it's for (overdue vs. lost/damaged)
- Whether an unresolved balance is blocking new checkouts

## Reading history

- Titles they've read before — useful if they forgot a title, and doubles as a reading log
- Optionally surfaced as a "books read this year" stat or reading streak

## Notifications / account activity

- Due-soon reminders, hold-ready alerts, overdue notices
- A lightweight in-app activity feed alongside (not instead of) email notifications

## Account settings

- Change password, update contact email
- View (read-only) member number/grade — students should not be able to edit their own grade or member type; that stays staff-controlled

## Nice-to-have, not essential

- Recommendations based on genre or reading history
- A separate "reading list" / wishlist distinct from active reservations
- Class or grade-level reading assignments tied to curriculum

## Explicitly out of scope for students

Anything that lets a student see or act on **other members'** data, or touch fine adjustments / copy and inventory status. Those stay behind staff-only authorization (librarian/administrator).

## Suggested build priority

Given a typical nav of `Dashboard`, `Catalog`, `My books`, `Account`, the two highest-value pages to add next are usually:

1. **My Books** — active loans + renew action
2. **My Holds** — reservations + cancel action
