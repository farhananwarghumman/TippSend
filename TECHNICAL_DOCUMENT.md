# GoldenSend — Technical Document

**Version:** 1.0  
**Date:** 19 May 2026  
**Author:** Development Session  
**Project Location:** `D:\GoldenSend\GoldenSendApp`

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Business Requirements Summary](#2-business-requirements-summary)
3. [Technology Stack](#3-technology-stack)
4. [Solution Structure](#4-solution-structure)
5. [Database Design](#5-database-design)
6. [Data Models](#6-data-models)
7. [Services Layer](#7-services-layer)
8. [Application Pages & Routes](#8-application-pages--routes)
9. [Authentication & Authorization](#9-authentication--authorization)
10. [Booking Wizard Flow](#10-booking-wizard-flow)
11. [Operator Dashboard](#11-operator-dashboard)
12. [Admin Area](#12-admin-area)
13. [Brand & UI System](#13-brand--ui-system)
14. [Session Management](#14-session-management)
15. [Seed Data](#15-seed-data)
16. [Pricing Logic](#16-pricing-logic)
17. [Order Reference & Tracking Token](#17-order-reference--tracking-token)
18. [Running the Application](#18-running-the-application)
19. [Database Connection](#19-database-connection)
20. [Default Credentials](#20-default-credentials)
21. [Roadmap / What to Build Next](#21-roadmap--what-to-build-next)

---

## 1. Project Overview

**GoldenSend** is a timed personal shopping and delivery service operating in South Tipperary, Ireland. The platform's defining feature is **exact-time delivery** — customers can book a delivery to arrive within a 15-minute window (±5 minutes), something no other Irish local delivery service offers combined with an unrestricted product catalogue.

The service operates on a **zero-inventory model**: GoldenSend purchases items on behalf of the customer from any local shop and delivers them at the precise time specified.

### Key Differentiators

- **Exact Moment tier**: ±5 minute delivery window guarantee
- **Any shop, any item**: Not limited to a product catalogue — customers describe what they want
- **Diaspora-friendly**: International senders (Irish abroad) can send gifts with a time-zone converter, paying in GBP, USD, or AUD
- **Personal card**: Every order includes a handwritten card on cream paper, tied with twine
- **Photo confirmation**: Every delivery includes a doorstep photo sent to the sender

### Service Area

Centred on Clonmel, Co. Tipperary — covering a 25 km radius across 10 towns with an estimated catchment of 45,000 people.

---

## 2. Business Requirements Summary

Sourced from the PRD (Product Requirements Document): `D:\GoldenSend\PRD Timed Delivery Service.docx`

### User Roles

| Role | Description |
|---|---|
| **Local Consumer** | Books via the public website. Receives WhatsApp notifications. |
| **Diaspora Sender** | Irish nationals abroad. Pays in foreign currency. Uses the time-zone helper. |
| **Recipient** | No account required. Receives optional SMS before delivery. |
| **B2B Business** | Local shops/solicitors/pharmacies using GoldenSend as a shared delivery driver on a monthly retainer. |
| **Operator/Admin** | Founder-driver. Manages the day's orders, route, photos, and status updates. |

### Delivery Tiers

| Tier | Window | Zone A | Zone B | Zone C |
|---|---|---|---|---|
| Standard | ±90 min | €8 | €12 | €15 |
| Precision | ±30 min | €12 | €18 | €22 |
| Exact Moment | ±5 min | €18 | €25 | €30 |

Additional: +€8 surcharge for any evening (after 18:00) or Sunday delivery.  
Item markup: transparent 8% on top of shop price.

### Business Rules

- Maximum item value: €500 (Phase 1 insurance limit)
- Capacity cap: 15 deliveries per day (single driver, Phase 1)
- If the operator misses the guaranteed time window, the tier premium is automatically refunded
- Items are purchased on the day — zero inventory held
- Photo required on every delivery
- Card message capped at 220 characters

---

## 3. Technology Stack

| Layer | Technology | Version |
|---|---|---|
| Framework | ASP.NET Core Razor Pages | 7.0 |
| ORM | Entity Framework Core | 7.0.20 |
| Database Provider | Microsoft.EntityFrameworkCore.SqlServer | 7.0.20 |
| Database Server | Microsoft SQL Server Developer | 16.0.1000.6 (SQL Server 2022) |
| Authentication | ASP.NET Core Identity | Built-in |
| UI Framework | Bootstrap | 5.x (bundled with template) |
| Frontend | Vanilla JavaScript (no SPA framework) | — |
| Session Storage | ASP.NET Core Distributed Memory Cache | Built-in |
| Language | C# | 11 |
| Build Tool | MSBuild / dotnet CLI | .NET SDK 7.0.400 |

### NuGet Packages

```xml
<PackageReference Include="Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore" Version="7.0.20" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="7.0.20" />
<PackageReference Include="Microsoft.AspNetCore.Identity.UI" Version="7.0.20" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="7.0.20" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="7.0.20" />
```

---

## 4. Solution Structure

```
D:\GoldenSend\
├── GoldenSendApp.sln
└── GoldenSendApp\
    ├── Program.cs                          # App startup, DI, auth, seeding
    ├── appsettings.json                    # Connection string, logging
    ├── appsettings.Development.json
    │
    ├── Models\
    │   ├── Enums.cs                        # OrderStatus, DeliveryTier, PricingZone
    │   ├── Order.cs                        # Core order entity
    │   ├── Shop.cs                         # Local shop entity
    │   ├── B2BAccount.cs                   # Business contract entity
    │   └── BookingSession.cs               # Session state for booking wizard (not DB)
    │
    ├── Data\
    │   ├── ApplicationDbContext.cs         # EF Core DbContext + seed data
    │   └── Migrations\
    │       └── 20260519125537_InitialCreate.*
    │
    ├── Services\
    │   ├── PricingService.cs               # Fee calculation, surcharges
    │   └── OrderService.cs                 # Order creation & status updates
    │
    ├── Extensions\
    │   └── SessionExtensions.cs            # Generic JSON session get/set
    │
    ├── Pages\
    │   ├── Index.cshtml(.cs)               # Home / marketing landing page
    │   ├── Shared\
    │   │   ├── _Layout.cshtml              # Site-wide layout (nav + footer)
    │   │   ├── _LoginPartial.cshtml        # Auth nav partial
    │   │   └── _ValidationScriptsPartial.cshtml
    │   │
    │   ├── Book\                           # 5-step booking wizard
    │   │   ├── Step1.cshtml(.cs)           # Shop + item selection
    │   │   ├── Step2.cshtml(.cs)           # Sender + recipient details
    │   │   ├── Step3.cshtml(.cs)           # Date, zone, tier, time slot
    │   │   ├── Step4.cshtml(.cs)           # Card message + order summary
    │   │   └── Confirmation.cshtml(.cs)    # Post-order confirmation
    │   │
    │   ├── Track\
    │   │   └── Index.cshtml(.cs)           # Public order tracking by token
    │   │
    │   ├── Diaspora\
    │   │   └── Index.cshtml(.cs)           # International sender landing page
    │   │
    │   ├── Operator\
    │   │   └── Dashboard.cshtml(.cs)       # Full-screen ops dashboard [Auth]
    │   │
    │   └── Admin\                          # Protected admin area [Auth]
    │       ├── Index.cshtml(.cs)           # Admin hub
    │       ├── Orders\Index.cshtml(.cs)    # Order list + status management
    │       ├── Shops\Index.cshtml(.cs)     # Shop CRUD
    │       ├── B2BAccounts\Index.cshtml(.cs) # B2B account CRUD
    │       └── Pricing\Index.cshtml(.cs)  # Pricing reference (read-only)
    │
    ├── Areas\
    │   └── Identity\Pages\                 # ASP.NET Identity scaffolded pages
    │       └── _ViewStart.cshtml
    │
    └── wwwroot\
        ├── css\
        │   └── brand.css                   # All GoldenSend custom styles
        ├── js\
        │   └── site.js
        ├── uploads\                        # Delivery photos (created at runtime)
        └── lib\                            # Bootstrap 5, jQuery, validation
```

---

## 5. Database Design

### Server

- **Server:** `FARHAN` (SQL Server 2022 Developer Edition, version 16.0.1000.6)
- **Authentication:** Windows Authentication (`FARHAN\farha`)
- **Database:** `GoldenSendDb`
- **Connection String:**
  ```
  Server=FARHAN;Database=GoldenSendDb;Trusted_Connection=True;
  MultipleActiveResultSets=true;TrustServerCertificate=True
  ```

### Tables

The database contains 11 tables total — 3 application tables + 8 ASP.NET Identity tables.

#### Application Tables

**`Orders`** — Core order entity

| Column | Type | Nullable | Notes |
|---|---|---|---|
| Id | int (PK, identity) | NOT NULL | Auto-increment |
| Reference | nvarchar(30) | NOT NULL | `GS·DDMMYY·TIER·ZONE` |
| TrackingToken | nvarchar(64) | NOT NULL | GUID (no hyphens), for public link |
| SenderName | nvarchar(150) | NOT NULL | |
| SenderEmail | nvarchar(200) | NOT NULL | |
| SenderPhone | nvarchar(30) | NOT NULL | |
| SenderCountry | nvarchar(5) | NOT NULL | Default `IE` |
| ShopId | int (FK) | NULL | FK → Shops.Id |
| ShopNameOverride | nvarchar(200) | NOT NULL | Used when no shop selected |
| ItemDescription | nvarchar(500) | NOT NULL | |
| EstimatedItemPrice | decimal(10,2) | NOT NULL | Customer estimate |
| BudgetCap | decimal(10,2) | NOT NULL | Operator stops at this |
| ActualItemPrice | decimal(10,2) | NULL | Filled after collection |
| MarkupPct | decimal(5,2) | NOT NULL | Default 8.00 |
| RecipientName | nvarchar(150) | NOT NULL | |
| RecipientAddress | nvarchar(300) | NOT NULL | |
| RecipientTown | nvarchar(100) | NOT NULL | |
| RecipientPhone | nvarchar(30) | NOT NULL | |
| DeliveryDate | datetime2 | NOT NULL | Date of delivery |
| DeliveryWindowStart | time | NOT NULL | Start of delivery window |
| Tier | int | NOT NULL | Enum: 0=Standard, 1=Precision, 2=ExactMoment |
| Zone | int | NOT NULL | Enum: 0=A, 1=B, 2=C |
| IsEvening | bit | NOT NULL | True if window start ≥ 18:00 |
| IsSunday | bit | NOT NULL | True if Sunday delivery |
| CardMessage | nvarchar(220) | NOT NULL | Max 220 chars |
| ServiceFee | decimal(10,2) | NOT NULL | Tier+zone fee |
| EveningSundaySurcharge | decimal(10,2) | NOT NULL | 0 or 8 |
| ItemMarkupAmount | decimal(10,2) | NOT NULL | 8% of item price |
| Total | decimal(10,2) | NOT NULL | Full order total |
| Status | int | NOT NULL | Enum (see below) |
| DeliveryPhotoPath | nvarchar(500) | NULL | Relative path in `/uploads/` |
| DriverNotes | nvarchar(500) | NULL | Operator notes |
| Rating | int | NULL | 1–5 stars, set post-delivery |
| B2BAccountId | int (FK) | NULL | FK → B2BAccounts.Id |
| CreatedAt | datetime2 | NOT NULL | UTC |
| ConfirmedAt | datetime2 | NULL | UTC |
| CollectedAt | datetime2 | NULL | UTC |
| InTransitAt | datetime2 | NULL | UTC |
| DeliveredAt | datetime2 | NULL | UTC |

**`Shops`** — Local shop directory

| Column | Type | Nullable |
|---|---|---|
| Id | int (PK) | NOT NULL |
| Name | nvarchar(200) | NOT NULL |
| Address | nvarchar(300) | NOT NULL |
| Town | nvarchar(100) | NOT NULL |
| OpeningHours | nvarchar(100) | NOT NULL |
| ContactPhone | nvarchar(30) | NULL |
| PaymentNotes | nvarchar(300) | NULL |
| IsActive | bit | NOT NULL |

**`B2BAccounts`** — Business delivery contracts

| Column | Type | Nullable |
|---|---|---|
| Id | int (PK) | NOT NULL |
| BusinessName | nvarchar(200) | NOT NULL |
| ContactName | nvarchar(150) | NOT NULL |
| ContactEmail | nvarchar(200) | NOT NULL |
| ContactPhone | nvarchar(30) | NOT NULL |
| Address | nvarchar(300) | NOT NULL |
| Town | nvarchar(100) | NOT NULL |
| MonthlyRetainer | decimal(10,2) | NOT NULL |
| PerDeliveryRate | decimal(10,2) | NOT NULL |
| IsActive | bit | NOT NULL |
| CreatedAt | datetime2 | NOT NULL |

#### Identity Tables (ASP.NET Core Identity)

| Table | Purpose |
|---|---|
| AspNetUsers | Application user accounts |
| AspNetRoles | Roles (Admin, Operator) |
| AspNetUserRoles | Many-to-many: users ↔ roles |
| AspNetUserClaims | User-specific claims |
| AspNetRoleClaims | Role-based claims |
| AspNetUserLogins | External login providers |
| AspNetUserTokens | Tokens (refresh, 2FA, etc.) |

### Foreign Keys

```
Orders.ShopId         → Shops.Id
Orders.B2BAccountId   → B2BAccounts.Id
```

### Indexes

```
IX_Orders_ShopId
IX_Orders_B2BAccountId
```

---

## 6. Data Models

### Enums (`Models/Enums.cs`)

```csharp
public enum OrderStatus
{
    Queued,       // 0 — created, not yet confirmed
    Confirmed,    // 1 — operator has accepted
    Collected,    // 2 — item picked up from shop
    InTransit,    // 3 — on the way to recipient
    Delivered,    // 4 — doorstep photo taken
    Failed        // 5 — delivery unsuccessful
}

public enum DeliveryTier
{
    Standard,     // 0 — ±90 minute window
    Precision,    // 1 — ±30 minute window
    ExactMoment   // 2 — ±5 minute window
}

public enum PricingZone
{
    A,  // 0 — Clonmel town, 0–5 km
    B,  // 1 — Inner ring, 9–20 km
    C   // 2 — Outer ring, 20–25 km
}
```

### Order (`Models/Order.cs`)

The central entity of the system. Contains full sender, recipient, item, delivery, pricing, and fulfillment data. Also exposes computed properties (not persisted) used in views:

- `TierLabel` — human-readable tier string
- `TierCode` — 3-letter code (STD/PRE/EXM) for reference numbers
- `WindowLabel` — e.g. `"18:30–18:35"`
- `StatusBadgeClass` — Bootstrap badge class for colour-coded status display

### BookingSession (`Models/BookingSession.cs`)

Not a database entity. A plain C# class serialised to JSON and stored in `ISession` during the booking wizard. Carries state between the 4 booking steps without making a database write until the customer confirms on Step 4.

Fields mirror the Order entity fields needed during booking (shop, item, sender, recipient, date/time, tier, zone, card message).

---

## 7. Services Layer

### PricingService (`Services/PricingService.cs`)

Stateless service registered as `Scoped`. Contains all pricing logic.

**Service fee table (hardcoded):**

```
              Zone A   Zone B   Zone C
Standard      €8       €12      €15
Precision     €12      €18      €22
ExactMoment   €18      €25      €30
```

**Key methods:**

| Method | Description |
|---|---|
| `GetServiceFee(tier, zone)` | Returns the base service fee |
| `GetSurcharge(date, windowStart)` | Returns €8 if evening (≥18:00) or Sunday, else €0 |
| `Calculate(tier, zone, date, window, itemPrice)` | Returns full tuple: (fee, surcharge, markup, total) |
| `GetPrecisionMinutes(tier)` | Static — returns 90/30/5 for each tier |
| `GetTierBadgeClass(tier)` | Static — returns CSS class name for tier badge |

### OrderService (`Services/OrderService.cs`)

Registered as `Scoped`. Handles database writes.

**Key methods:**

| Method | Description |
|---|---|
| `CreateFromSessionAsync(session)` | Converts a `BookingSession` into a saved `Order`. Calls `PricingService.Calculate`, generates `TrackingToken` (GUID), generates `Reference`, saves to DB. |
| `UpdateStatusAsync(id, status, notes, photoPath)` | Updates order status and sets the appropriate timestamp (ConfirmedAt, CollectedAt, etc.). Optionally persists driver notes and photo path. |

**Order reference format:**  
`GS·{DDMMYY}·{TIER_CODE}·{ZONE}` — e.g. `GS·190526·EXM·A`

---

## 8. Application Pages & Routes

### Public Pages (no authentication required)

| Route | Page | Description |
|---|---|---|
| `/` | `Pages/Index` | Marketing home page — hero, how-it-works, pricing grid, B2B & diaspora callouts, 10-town map |
| `/Diaspora/Index` | `Pages/Diaspora/Index` | International sender landing — time-zone converter, most-sent gifts, testimonial |
| `/Book/Step1` | `Pages/Book/Step1` | Booking wizard Step 1: shop + item description |
| `/Book/Step2` | `Pages/Book/Step2` | Booking wizard Step 2: sender + recipient details |
| `/Book/Step3` | `Pages/Book/Step3` | Booking wizard Step 3: date, zone, tier, time slot |
| `/Book/Step4` | `Pages/Book/Step4` | Booking wizard Step 4: card message + order summary |
| `/Book/Confirmation/{token}` | `Pages/Book/Confirmation` | Post-order confirmation with timeline |
| `/Track/Index?token={token}` | `Pages/Track/Index` | Live order tracking — countdown, timeline, photo, rating |

### Protected Pages (require login)

| Route | Role Required | Description |
|---|---|---|
| `/Operator/Dashboard` | Admin or Operator | Full-screen 3-column ops view |
| `/Admin/Index` | Admin only | Admin navigation hub |
| `/Admin/Orders/Index` | Admin only | Order list with filters and status management |
| `/Admin/Shops/Index` | Admin only | Shop directory CRUD |
| `/Admin/B2BAccounts/Index` | Admin only | B2B account CRUD |
| `/Admin/Pricing/Index` | Admin only | Pricing tier reference (read-only) |

### Identity Routes (ASP.NET Identity scaffolded)

| Route | Description |
|---|---|
| `/Identity/Account/Login` | Login page |
| `/Identity/Account/Register` | Register page |
| `/Identity/Account/Logout` | Logout |

---

## 9. Authentication & Authorization

### Configuration (`Program.cs`)

```csharp
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();
```

Email confirmation is **disabled** (RequireConfirmedAccount = false) for Phase 1 simplicity.

### Authorization Policies

```csharp
options.AddPolicy("RequireOperator", p => p.RequireRole("Admin", "Operator"));
options.AddPolicy("RequireAdmin", p => p.RequireRole("Admin"));
```

### Folder-Level Protection

```csharp
options.Conventions.AuthorizeFolder("/Operator", "RequireOperator");
options.Conventions.AuthorizeFolder("/Admin", "RequireAdmin");
```

All pages under `/Operator/` require at minimum the `Operator` role.  
All pages under `/Admin/` require the `Admin` role.

### Roles

| Role | Access |
|---|---|
| `Admin` | Full access — Admin area + Operator dashboard |
| `Operator` | Operator dashboard only |

### Startup Seeding

On every application startup, `Program.cs` runs a seeding block that:
1. Ensures the `Admin` and `Operator` roles exist (creates if missing)
2. Checks for `admin@goldensend.ie` — creates the account if it doesn't exist
3. Assigns the `Admin` role to that account

This is idempotent — safe to run on every startup.

---

## 10. Booking Wizard Flow

The booking wizard uses **ASP.NET Core Session** to carry state across 4 HTTP requests without writing to the database until the final confirmation.

### Session Key: `"Booking"`

A `BookingSession` object is serialised to JSON using `System.Text.Json` and stored via `SessionExtensions.SetObject<T>` / `GetObject<T>`.

### Step Flow

```
Step 1 (Shop & Item)
  ↓ POST saves to session
Step 2 (Sender & Recipient)
  ↓ POST saves to session
Step 3 (Date / Zone / Tier / Time Slot)
  ↓ POST saves to session
Step 4 (Card Message + Summary preview)
  ↓ POST → calls OrderService.CreateFromSessionAsync()
       → clears session
       → redirects to /Book/Confirmation/{token}
Confirmation page (reads Order from DB by TrackingToken)
```

### Step 3 Detail — Time Slot Grid

- Slots generated server-side: every 30 minutes from 09:00 to 20:30
- Evening slots (≥18:00) are visually highlighted and labelled "+€8"
- Selected slot stored as `DeliveryHour` + `DeliveryMinute` integers
- Live price preview recalculated server-side on each POST

### Guard Clause

Every step except Step 1 reads the session on `OnGet`. If the session is missing (e.g. expired or direct navigation), the user is redirected back to Step 1.

---

## 11. Operator Dashboard

Located at `/Operator/Dashboard` — protected by the `RequireOperator` policy.

### Layout

The dashboard uses its **own full-screen layout** (not `_Layout.cshtml`). It is a CSS Grid with three fixed columns:

```
┌─────────────┬──────────────────────────────┬────────────────┐
│  Sidebar    │  Order Queue (main panel)     │  Order Detail  │
│  240px      │  flexible                     │  380px         │
└─────────────┴──────────────────────────────┴────────────────┘
```

### Sidebar

- GoldenSend logo / brand name
- Navigation links (Today, All Orders, Shops, B2B Accounts, Public Site)
- Today's delivery stats: total, Exact/Precision/Standard breakdown, delivered count
- Capacity warning if order count exceeds 15
- Revenue today (delivered orders only)
- Sign out link

### Order Queue (Main Panel)

- Sticky top bar with date, metric pills (delivered/total, revenue), New Order button
- Filter bar: All / Exact / Precision / Standard / B2B
- Order rows: window time, item description, route (shop→town), tier badge, status badge, total
- Clicking a row selects it and loads detail in the right panel (via query string `?SelectedOrderId=N`)

### Detail Panel

For the selected order, shows:
- Tier badge + status badge
- Item description + order reference
- Pickup shop with opening hours and payment notes
- Delivery address and recipient contact
- Budget cap
- Card message (styled as lined paper)
- 4 financial mini-stats (service fee, markup, surcharge, total)
- Delivery photo (if uploaded)
- **Status update form**: dropdown to change status, optional driver notes, photo upload
- **WhatsApp button**: pre-filled WhatsApp link to message the sender

### POST Handler: `OnPostUpdateStatusAsync`

1. Optionally saves an uploaded photo to `wwwroot/uploads/` with a GUID filename
2. Calls `OrderService.UpdateStatusAsync(orderId, newStatus, notes, photoPath)`
3. Redirects back to the dashboard with the same selected order

---

## 12. Admin Area

All pages under `/Admin/` — protected by the `RequireAdmin` policy.

### Admin Hub (`/Admin/Index`)

Navigation cards to the four admin sections.

### Orders (`/Admin/Orders/Index`)

- Filter by: free-text search (name/reference/item), specific date, status
- Table columns: reference, date, window, item, recipient, tier, status, total
- Per-row inline status update (select + button)
- Track link opens the public tracking page in a new tab

### Shops (`/Admin/Shops/Index`)

Full CRUD:
- **Create**: inline form at top of page — name, address, town (dropdown), opening hours, phone, payment notes
- **Read**: table of all shops ordered by town then name
- **Toggle Active**: button toggles `IsActive` flag (soft disable — shop hidden from booking form)
- **Delete**: with JavaScript confirmation dialog

### B2B Accounts (`/Admin/B2BAccounts/Index`)

Full CRUD (except delete — accounts are toggled inactive):
- **Create**: inline form — business name, contact details, address, town, monthly retainer, per-delivery rate
- **Read**: table of all accounts
- **Toggle Active**: activates or deactivates the account

### Pricing Reference (`/Admin/Pricing/Index`)

Read-only page displaying:
- Service fee matrix (3 tiers × 3 zones)
- Surcharge rules table
- Zone definitions with towns and distances
- Note pointing to `PricingService.cs` for code changes

---

## 13. Brand & UI System

### Brand Colours (`wwwroot/css/brand.css`)

All colours are defined as CSS custom properties on `:root`:

| Variable | Hex | Usage |
|---|---|---|
| `--gs-bog` | `#1F3A2E` | Primary dark green — navs, headers, CTAs |
| `--gs-cream` | `#F6F0E2` | Light background, text on dark |
| `--gs-gold` | `#C9A24B` | Accent — primary button, key highlights |
| `--gs-paper` | `#FAF6EC` | Page background |
| `--gs-ink` | `#14110C` | Body text |
| `--gs-claret` | `#9C2A1A` | Error states, evening slot highlight |
| `--gs-fog` | `#D4CDB8` | Borders, dividers, muted text |

### Typography

- **Display / headings**: `Newsreader` (Google Fonts, serif) — editorial, warm character
- **UI / body**: `system-ui` — fast-loading, native OS font
- **Numerals / time**: `Courier New` (monospace) — used for times, prices, order references

### Key CSS Components

| Class | Description |
|---|---|
| `.gs-nav` | Dark green sticky navigation bar |
| `.gs-hero` | Dark green hero section with cream text |
| `.btn-gold` | Gold primary button |
| `.btn-outline-cream` | Outlined cream button for dark backgrounds |
| `.tier-badge` | Small pill badge — `.tier-standard`, `.tier-precision`, `.tier-exact` |
| `.pricing-card` | Tier pricing card with `.featured` modifier |
| `.time-dial` | Dark green box showing delivery time in monospace gold |
| `.booking-step-card` | White card container for booking wizard steps |
| `.slot-grid` | 4-column grid of time slot buttons |
| `.card-message-area` | Lined-paper textarea for card messages |
| `.summary-row` | Key-value row used in order summaries |
| `.op-layout` | CSS Grid wrapper for the operator dashboard |
| `.tracking-status` | Dark green status card on the tracking page |
| `.countdown-timer` | Large monospace gold countdown on tracking page |
| `.diaspora-hero` | Gradient hero section for diaspora page |
| `.gift-card` | Hover-lift card for most-sent gifts |

---

## 14. Session Management

ASP.NET Core's built-in session middleware is used to persist booking wizard state.

### Configuration (`Program.cs`)

```csharp
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
// ...
app.UseSession(); // placed after UseRouting, before MapRazorPages
```

### SessionExtensions (`Extensions/SessionExtensions.cs`)

```csharp
session.SetObject<T>(key, value)   // Serialises T to JSON string
session.GetObject<T>(key)          // Deserialises or returns null
```

### Session Key

`"Booking"` — holds a `BookingSession` JSON object. Cleared immediately after `OrderService.CreateFromSessionAsync` completes successfully.

---

## 15. Seed Data

All seed data is embedded in `ApplicationDbContext.OnModelCreating` using EF Core's `HasData` API, so it is applied automatically as part of the initial migration.

### Shops (8 records)

| Id | Name | Town |
|---|---|---|
| 1 | Blooming Thing Floristry | Clonmel |
| 2 | Hickeys Bakery | Clonmel |
| 3 | Lonergan's Off Licence | Clonmel |
| 4 | Clonmel Pharmacy | Clonmel |
| 5 | Cahir Crafts & Gifts | Cahir |
| 6 | Carrick Delicatessen | Carrick-on-Suir |
| 7 | Golden Vale Butchers | Clonmel |
| 8 | Cashel Heritage Books | Cashel |

### B2B Accounts (3 records)

| Id | Business | Monthly Retainer | Per Delivery |
|---|---|---|---|
| 1 | O'Brien Solicitors | €150 | €8 |
| 2 | Clonmel Medical Centre | €200 | €10 |
| 3 | Slievenamon Hotel | €120 | €9 |

### Roles & Admin User (seeded at runtime in `Program.cs`)

| Type | Value |
|---|---|
| Role | `Admin` |
| Role | `Operator` |
| User email | `admin@goldensend.ie` |
| User password | `Admin@12345` |
| User role | `Admin` |

---

## 16. Pricing Logic

All pricing lives in `Services/PricingService.cs`.

### Fee Matrix

```csharp
private static readonly decimal[,] ServiceFees =
{
    //          Zone A  Zone B  Zone C
    /* Standard */  { 8m,   12m,   15m },
    /* Precision */ { 12m,  18m,   22m },
    /* Exact    */  { 18m,  25m,   30m },
};
```

Access: `ServiceFees[(int)tier, (int)zone]`

### Surcharge

```csharp
bool isEvening = windowStart.Hours >= 18;
bool isSunday  = date.DayOfWeek == DayOfWeek.Sunday;
return (isEvening || isSunday) ? 8m : 0m;
```

Note: surcharge is €8 regardless of whether it's both evening AND Sunday — it does not stack.

### Item Markup

```
markupAmount = Math.Round(estimatedItemPrice * 0.08m, 2)
```

### Total

```
total = estimatedItemPrice + markupAmount + serviceFee + surcharge
```

---

## 17. Order Reference & Tracking Token

### Order Reference

Format: `GS·{DDMMYY}·{TIER_CODE}·{ZONE}`

| Segment | Source | Example |
|---|---|---|
| `GS` | Literal | `GS` |
| `DDMMYY` | `DeliveryDate.ToString("ddMMyy")` | `190526` |
| `TIER_CODE` | `STD` / `PRE` / `EXM` | `EXM` |
| `ZONE` | `A` / `B` / `C` | `A` |

Full example: **`GS·190526·EXM·A`**

Generated in `OrderService.GenerateReference()` just before the order is saved.

### Tracking Token

A GUID with hyphens stripped: `Guid.NewGuid().ToString("N")`

Example: `a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6`

This token is embedded in the public tracking URL:  
`/Track/Index?token=a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6`

And in the confirmation page URL:  
`/Book/Confirmation/a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6`

---

## 18. Running the Application

### Prerequisites

- [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7) — v7.0.400 or later
- SQL Server 2019 or 2022 (Developer Edition is fine) — accessible as `FARHAN`
- Windows Authentication access to the SQL Server instance

### First-Time Setup

```bash
# 1. Navigate to project
cd D:\GoldenSend\GoldenSendApp

# 2. Restore packages
dotnet restore

# 3. Apply database migration (creates GoldenSendDb on FARHAN)
dotnet-ef database update

# 4. Run the application
dotnet run
```

The application auto-seeds roles and the admin user on first run.

### Subsequent Runs

```bash
cd D:\GoldenSend\GoldenSendApp
dotnet run
```

Default URL: `http://localhost:5XXX` (port assigned dynamically — check console output for "Now listening on:").

### Fixed Port

To always use port 5100:

```bash
dotnet run --urls "http://localhost:5100"
```

### Installing the EF CLI Tool (if not already installed)

```bash
dotnet tool install --global dotnet-ef --version 7.0.20
```

---

## 19. Database Connection

### Current Configuration (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=FARHAN;Database=GoldenSendDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

### Verified Database State (as of 19 May 2026)

| Table | Row Count |
|---|---|
| Shops | 8 |
| B2BAccounts | 3 |
| Orders | 0 (empty — ready for live use) |
| AspNetRoles | 2 |
| AspNetUsers | 1 |
| AspNetUserRoles | 1 |

### Migration History

| Migration ID | Applied |
|---|---|
| `20260519125537_InitialCreate` | Yes |

---

## 20. Default Credentials

| Account | Email | Password | Role |
|---|---|---|---|
| Admin | `admin@goldensend.ie` | `Admin@12345` | Admin |

Login URL: `http://localhost:5100/Identity/Account/Login`

After login, the navigation bar shows a **Dashboard** button linking to `/Operator/Dashboard`.

---

## 21. Roadmap / What to Build Next

The following features are described in the PRD but not yet implemented in Phase 1:

### High Priority

| Feature | Notes |
|---|---|
| **Stripe payment integration** | Multi-currency (EUR/GBP/USD/AUD). Add `Stripe.net` NuGet package. Replace simulated payment in Step 4 with a Stripe Checkout session. |
| **WhatsApp Business API** | Automated notifications at 5 touchpoints (order received, confirmed, collected, in transit, delivered). Use Twilio or the Meta WhatsApp API. |
| **Email confirmation** | SendGrid integration for order confirmation emails with tracking link. Currently `RequireConfirmedAccount = false`. |
| **Delivery photo storage** | Currently saved to `wwwroot/uploads/` (local disk). Should move to Azure Blob Storage or AWS S3 for production. |
| **Real-time order tracking** | Replace the static countdown with a SignalR hub so the tracking page updates live without refresh. |

### Medium Priority

| Feature | Notes |
|---|---|
| **Google Maps route optimisation** | Use the Distance Matrix + Directions API to order the day's stops optimally on the operator dashboard. |
| **Order editing** | Allow operator to edit item description, price, or time window after order confirmation. |
| **Recipient SMS notification** | Optional SMS to recipient 15 minutes before delivery (Twilio SMS). |
| **B2B invoicing** | Generate monthly PDF invoices for B2B accounts. |
| **Operator role self-service** | Let admin create Operator accounts via the admin panel without using ASP.NET Identity scaffolding directly. |

### Future (Phase 2+)

| Feature | Notes |
|---|---|
| **Native mobile app** | iOS/Android app (React Native or MAUI) for both the customer booking flow and operator route management. |
| **GPS live tracking** | Real driver location on the tracking map using a mobile app → SignalR → tracking page. |
| **Repeat order / favourites** | Customers can save frequent orders for quick re-booking. |
| **Multi-driver support** | Assign orders to specific drivers. Route planning per driver. |
| **Analytics dashboard** | Revenue trends, on-time delivery KPIs, order volume by tier/zone. |
| **Franchise / partner model** | White-label the platform for other Irish towns. |

---

*End of Technical Document — GoldenSend v1.0*
