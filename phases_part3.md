# ZeroDawn Roadmap — Phases 11–15, Final Sections

## Phase 11 · Shared UI Components

**Goal**: Reusable Blazor components for layouts, forms, feedback, navigation — CSS/SVG only, no Lottie, no third-party UI libs.
**Why**: Every page needs these. Build once in Shared, use in WASM + MAUI.
**Prerequisites**: Phase 0 (folder structure).

### Task 11.1 — CSS Design System + Theme Variables

**Prompt for AI model:**
```
In ZeroDawn.Shared/wwwroot/app.css, replace content with a CSS design system.

Define CSS custom properties for a dark + light theme:
:root {
    /* Light theme (default) */
    --color-bg: #f8f9fa;
    --color-surface: #ffffff;
    --color-primary: #4f46e5;
    --color-primary-hover: #4338ca;
    --color-secondary: #6b7280;
    --color-success: #22c55e;
    --color-warning: #f59e0b;
    --color-error: #ef4444;
    --color-text: #1f2937;
    --color-text-muted: #6b7280;
    --color-border: #e5e7eb;
    --shadow-sm: 0 1px 2px rgba(0,0,0,0.05);
    --shadow-md: 0 4px 6px rgba(0,0,0,0.1);
    --shadow-lg: 0 10px 15px rgba(0,0,0,0.1);
    --radius-sm: 0.375rem;
    --radius-md: 0.5rem;
    --radius-lg: 0.75rem;
    --font-family: 'Inter', system-ui, sans-serif;
    --transition-fast: 150ms ease;
    --transition-normal: 250ms ease;
}

[data-theme="dark"] {
    --color-bg: #111827;
    --color-surface: #1f2937;
    --color-text: #f9fafb;
    --color-text-muted: #9ca3af;
    --color-border: #374151;
    --shadow-sm: 0 1px 2px rgba(0,0,0,0.3);
    --shadow-md: 0 4px 6px rgba(0,0,0,0.4);
    --shadow-lg: 0 10px 15px rgba(0,0,0,0.4);
}

Add base styles:
- Reset + box-sizing: border-box
- Body: font-family var(--font-family), background var(--color-bg), color var(--color-text)
- Button base styles with primary/secondary/danger variants
- Input/select/textarea base styles
- Card class with surface background, border, shadow, radius
- Utility classes: .text-center, .text-muted, .mt-1 through .mt-4, .mb-1 through .mb-4, .flex, .gap-2, .gap-4

Add Google Fonts link in ZeroDawn.Web/Components/App.razor <head>:
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />

And in ZeroDawn/wwwroot/index.html <head> (same link).

Rules:
- CSS variables only — no SCSS, no Tailwind, no third-party CSS framework.
- Keep Bootstrap reference for now (gradual migration), but new components use custom CSS.
- Do NOT remove existing Bootstrap CSS link.
- Report: CSS variables defined, base styles, files changed.
```

**Expected output**: Complete CSS design system.
**Acceptance**: Both light and dark themes work. `[data-theme="dark"]` on body toggles theme.

---

### Task 11.2 — Layout Components (MainLayout, AuthLayout, NavMenu, Sidebar)

**Prompt for AI model:**
```
In ZeroDawn.Shared/, update/create layout components:

1. Layout/AuthLayout.razor:
A centered layout for login/register/forgot-password pages.
- Centered card on gradient background.
- App logo area (text placeholder for now).
- Slot for child content via @Body.
- Responsive: full-width on mobile, max-width 440px on desktop.
Use CSS isolation file.

2. Layout/MainLayout.razor (update existing):
A sidebar + topbar layout for authenticated pages.
- Collapsible sidebar on the left (250px expanded, 64px collapsed).
- Top bar with: user avatar area, user name, role badge, theme toggle, language selector, logout.
- Main content area with padding.
- Responsive: sidebar becomes overlay drawer on mobile.
- Include <ToastContainer /> for global toast notifications.
Use CSS isolation file.

3. Components/Layout/Sidebar.razor:
- Nav links grouped by role: common links, admin links, super admin links.
- Use Roles constants from ZeroDawn.Shared.Core.Constants for visibility.
- Active link highlighting via NavLink.
- Collapse/expand toggle.
CSS isolation file.

4. Components/Layout/TopBar.razor:
- User info display (name, role badge).
- Theme toggle button (calls JS interop to set data-theme attribute on <html>).
- Language selector dropdown.
- Logout button.
CSS isolation file.

5. Components/Layout/ThemeToggle.razor:
- Toggle between light/dark by setting data-theme on document.documentElement.
- Persist preference in localStorage.
- Use minimal JS interop (only for DOM attribute setting + localStorage).
CSS isolation file.

Rules:
- All components in ZeroDawn.Shared so they work in WASM and MAUI.
- CSS isolation files for each component.
- Responsive design with CSS media queries.
- Use CSS variables from the design system.
- SVG icons or Unicode symbols only — no icon library dependency.
- Do NOT use JavaScript except for ThemeToggle localStorage + DOM attribute.
- Do NOT use third-party component libraries.
- Report: components created, layout structure, responsive behavior.
```

**Expected output**: Complete layout system.
**Acceptance**: App renders with sidebar + topbar. Theme toggle works. Responsive on mobile.

---

### Task 11.3 — Form Controls + Feedback Components

**Prompt for AI model:**
```
In ZeroDawn.Shared/Components/, create these reusable components:

Forms/:
1. FormField.razor — wrapper: label + input + validation message.
   Parameters: Label, For (expression), HelpText. Renders <label>, @ChildContent, <ValidationMessage>.
2. FormInput.razor — styled <InputText> with icon slot.
3. FormPasswordInput.razor — password input with show/hide toggle.
4. FormSelect.razor — styled <InputSelect> wrapper.
5. FormCheckbox.razor — styled checkbox with label.
6. SubmitButton.razor — button with loading spinner state.
   Parameters: Text, LoadingText, IsLoading, IsDisabled.

Feedback/:
(Toast and ErrorDisplay already created in Phase 5.3)
7. LoadingSpinner.razor — CSS-only spinning animation. Parameter: Size (small/medium/large).
8. Skeleton.razor — CSS skeleton loading placeholder. Parameters: Width, Height, Rounded.
9. EmptyState.razor — "No results" display with icon + message + optional action button.
10. ConfirmDialog.razor — modal dialog with confirm/cancel.
    Parameters: Title, Message, ConfirmText, CancelText, OnConfirm, OnCancel, IsVisible.

Common/:
11. PageHeader.razor — page title + optional breadcrumb + action buttons area.
12. Badge.razor — small label with color variants (info, success, warning, error).
13. Pagination.razor — page navigation for paged data. Parameters: CurrentPage, TotalPages, OnPageChanged.
14. SearchBar.razor — input with debounced search. Parameters: Placeholder, OnSearch, DebounceMs.

All components must:
- Use CSS isolation files.
- Use CSS variables from the design system.
- Be fully functional without JavaScript (except SearchBar debounce uses Timer).
- Have no dependency on third-party packages.

Rules:
- Do NOT modify existing pages or layouts.
- Do NOT create pages — only reusable components.
- Report: all components created, parameter signatures, files.
```

**Expected output**: ~14 reusable components.
**Acceptance**: Each component renders correctly in isolation. No JS dependencies except where noted.

---

## Phase 12 · Auth Pages

**Goal**: All authentication pages using shared components and auth service.
**Prerequisites**: Phase 4 (client auth infra) + Phase 11 (UI components).

### Task 12.1 — Auth Pages

**Prompt for AI model:**
```
In ZeroDawn.Shared/Pages/Auth/ (new folder), create these Blazor pages using AuthLayout:

1. Login.razor — @page "/login"
   - EditForm with LoginRequest model.
   - Email + Password fields using FormInput/FormPasswordInput.
   - SubmitButton with loading state.
   - "Forgot password?" link.
   - "Register" link (conditionally shown based on AllowSelfRegistration).
   - On submit: call IAuthService.LoginAsync, handle success (navigate to "/"), handle errors (show toast).
   - On success: notify auth state provider.

2. Register.razor — @page "/register"
   - EditForm with RegisterRequest model.
   - FullName, Email, Password, ConfirmPassword fields.
   - On submit: call IAuthService.RegisterAsync.
   - If email confirmation required: show "Check your email" message.
   - If not: auto-login and redirect.

3. ForgotPassword.razor — @page "/forgot-password"
   - EditForm with ForgotPasswordRequest.
   - On submit: call IAuthService.ForgotPasswordAsync.
   - Always show success message (do not reveal if email exists).

4. ResetPassword.razor — @page "/reset-password"
   - Read email + token from query string.
   - EditForm with ResetPasswordRequest (NewPassword, ConfirmPassword).
   - On submit: call IAuthService.ResetPasswordAsync.

5. ConfirmEmail.razor — @page "/confirm-email"
   - Read userId + token from query string.
   - Auto-submit on init.
   - Show success or error.

6. ResendConfirmation.razor — @page "/resend-confirmation"
   - EditForm with ResendConfirmationRequest.
   - On submit: always show success toast.

7. ChangePassword.razor — @page "/change-password"
   - EditForm with ChangePasswordRequest.
   - [Authorize] protected.

All pages must:
- Use AuthLayout as the layout (@layout AuthLayout).
- Use <DataAnnotationsValidator /> + <ValidationSummary />.
- Use shared form components (FormField, FormInput, etc.).
- Use IToastService for success/error feedback.
- Handle loading state (disable submit during API call).
- Wrap API calls in try/catch, show ErrorDisplay on failure.
- Use IStringLocalizer<SharedResources> for all visible text.
- Be responsive.

Rules:
- Do NOT modify any existing pages.
- Do NOT modify controllers or services.
- Do NOT create new services — use existing IAuthService interface.
- Report: pages created, routes, layout used, localization usage.
```

**Expected output**: 7 auth pages, all routable and functional.
**Acceptance**: Login → token stored → redirect to home. Register → email flow. All auth pages render and submit.

---

## Phase 13 · Dashboard & Admin Pages

**Goal**: Role-based dashboards, admin controls, user management, error logs, profile, about, contact, 404.
**Prerequisites**: Phase 12 (auth pages work, user can log in).

### Task 13.1 — User/Admin DTOs + API Endpoints

**Prompt for AI model:**
```
1. In ZeroDawn.Shared/Contracts/Users/, create:
   UserDto.cs: Id, FullName, Email, Roles (List<string>), IsActive, CreatedAt, LastLoginAt, EmailConfirmed.
   UpdateProfileRequest.cs: FullName (Required, MaxLength 100).
   UpdateUserStatusRequest.cs: UserId, IsActive.
   AssignRoleRequest.cs: UserId, Role.

2. In ZeroDawn.Shared/Contracts/Common/, create:
   ErrorLogDto.cs: Id, ReferenceNumber, Message, StackTrace, Source, InnerException, UserId, RequestPath, CorrelationId, CreatedAt.

3. In ZeroDawn.Web/Controllers/, create:
   UsersController.cs [ApiController, Route("api/users"), Authorize]:
   - [GET] "" — [Authorize(Roles = "Admin,SuperAdmin")] — returns PagedResponse<UserDto>.
   - [GET] "{id}" — [Authorize(Roles = "Admin,SuperAdmin")] — returns single UserDto.
   - [GET] "profile" — returns current user's UserDto.
   - [PUT] "profile" — update current user's FullName.
   - [PUT] "{id}/status" — [Authorize(Roles = "Admin,SuperAdmin")] — toggle IsActive.
   - [PUT] "{id}/role" — [Authorize(Roles = "SuperAdmin")] — assign role.

   AdminController.cs [ApiController, Route("api/admin"), Authorize(Roles = "SuperAdmin")]:
   - [GET] "error-logs" — returns PagedResponse<ErrorLogDto> with search/filter.
   - [GET] "admins" — returns users in Admin role.
   - [PUT] "admins/{id}/role" — promote/demote admin.

All endpoints:
- Validate ModelState.
- Use try/catch with logging.
- Return ApiResponse<T>.
- Use transactions for multi-entity operations.
- Never log PII.

Rules:
- Do NOT modify existing auth endpoints.
- Report: DTOs created, endpoints, authorization rules.
```

**Expected output**: User/admin DTOs + 2 controllers.
**Acceptance**: GET /api/users returns paged users. Only authorized roles can access.

---

### Task 13.2 — Dashboard + Admin + Static Pages

**Prompt for AI model:**
```
In ZeroDawn.Shared/Pages/, create these pages using MainLayout:

Dashboards/:
1. HomeSuperAdmin.razor — @page "/dashboard/superadmin"
   [Authorize(Roles = "SuperAdmin")]
   Cards: total users, total admins, recent errors count, system status.
   Quick links to error logs, user management, admin management.
   Use demo/placeholder data for now.

2. HomeAdmin.razor — @page "/dashboard/admin"
   [Authorize(Roles = "Admin")]
   Cards: total users, recent activity.
   Quick link to user management.

3. HomeUser.razor — @page "/dashboard"
   [Authorize(Roles = "User")]
   Welcome message with user name. Profile link.

4. Home.razor — Update existing @page "/"
   If authenticated: redirect to role-appropriate dashboard.
   If not: redirect to /login.

Admin/:
5. UserManagement.razor — @page "/admin/users"
   [Authorize(Roles = "Admin,SuperAdmin")]
   Table of users with search, pagination, status toggle, role assignment.
   Use Pagination, SearchBar, Badge components.

6. AdminManagement.razor — @page "/admin/admins"
   [Authorize(Roles = "SuperAdmin")]
   Table of admins with promote/demote.

7. ErrorLogs.razor — @page "/admin/error-logs"
   [Authorize(Roles = "SuperAdmin")]
   Table with columns: ReferenceNumber, Message (truncated), Path, CreatedAt.
   Expandable row for full details (stack trace, inner exception).
   Search by reference number. Pagination.

Profile/:
8. ProfileBasics.razor — @page "/profile"
   [Authorize]
   Display + edit full name. Read-only email, role, member since.
   Avatar placeholder (initials-based circle).

9. UserDetails.razor — @page "/admin/users/{UserId}"
   [Authorize(Roles = "Admin,SuperAdmin")]
   Full user details: name, email, roles, status, created, last login.
   Actions: toggle active, assign role.

Static/:
10. About.razor — @page "/about"
    App name, version, description placeholder.

11. ContactUs.razor — @page "/contact"
    Simple form (name, email, message) — no backend yet, just UI.

12. NotFound.razor — update existing @page "/not-found"
    Styled 404 page with illustration (SVG or CSS art), "Go Home" link.

All pages must:
- Use MainLayout (@layout MainLayout).
- Use shared components (PageHeader, SearchBar, Pagination, Badge, etc.).
- Use IStringLocalizer for text.
- Use loading states (LoadingSpinner, Skeleton).
- Handle errors with ErrorDisplay + toast.
- Be responsive.

Rules:
- Pages use IAuthApiClient/IUserApiClient (typed clients) for data.
- For user/admin API clients not yet created, create interface stubs in Shared/Services.
- Demo data is acceptable for dashboard cards until real APIs exist.
- Do NOT modify auth pages or layout components.
- Report: pages created, routes, auth requirements, data flow.
```

**Expected output**: 12 pages covering all requested screens.
**Acceptance**: Login as SuperAdmin → sees SA dashboard + nav links to all admin pages. Login as User → sees user dashboard. 404 page shows on unknown routes.

---

## Phase 14 · Storage & Connectivity (MAUI)

**Goal**: Abstracted secure storage, preferences, offline detection, graceful failure UX.
**Prerequisites**: Phase 4 (MAUI token store exists).

### Task 14.1 — Storage + Connectivity Abstractions

**Prompt for AI model:**
```
1. In ZeroDawn.Shared/Services/, add interfaces:

   IPreferencesService.cs:
   Task<string?> GetAsync(string key);
   Task SetAsync(string key, string value);
   Task RemoveAsync(string key);

   ISecureStorageService.cs (already exists as ITokenStorageService — extend or keep separate):
   Task<string?> GetSecureAsync(string key);
   Task SetSecureAsync(string key, string value);
   Task RemoveSecureAsync(string key);

   Move IConnectivityService interface to Shared (from ZeroDawn/Services):
   bool IsConnected { get; }
   event EventHandler<bool>? ConnectivityChanged;

2. MAUI implementations (ZeroDawn/Services/):
   MauiPreferencesService.cs — uses MAUI Preferences API.
   MauiSecureStorageService.cs — uses MAUI SecureStorage API.
   (MauiConnectivityService already created in Phase 4.)

3. WASM implementations (ZeroDawn.Web.Client/Services/):
   BrowserPreferencesService.cs — uses localStorage via IJSRuntime.
   WebConnectivityService.cs — always returns IsConnected = true (WASM is always online if the page loaded).

4. In ZeroDawn.Shared/Components/Feedback/, create:
   OfflineBanner.razor:
   - Injects IConnectivityService.
   - Shows a yellow/red banner at top when offline.
   - Auto-hides when online again.
   - CSS isolation.

Register implementations in respective Program.cs files.

Rules:
- Interfaces in Shared, implementations in host projects.
- Wrap MAUI SecureStorage calls in try/catch (can fail on some platforms).
- Do NOT modify existing services.
- Report: interfaces, implementations, registrations, files.
```

**Expected output**: Storage + connectivity abstractions + offline banner.
**Acceptance**: MAUI app detects offline state. WASM ignores connectivity. OfflineBanner shows when disconnected.

---

## Phase 15 · Documentation

**Goal**: Five documentation files explaining how to run, deploy, understand, and hand off the project.
**Prerequisites**: Phase 13 (all features exist).

### Task 15.1 — Documentation Files

**Prompt for AI model:**
```
In the solution root (q:\Work\ZeroDawn\ZeroDawn\), create these markdown files:

1. RUNNING.md:
   - Prerequisites (SDK version, tools).
   - How to clone and restore.
   - How to set up User Secrets (dotnet user-secrets set "Jwt:Secret" "your-secret-here").
   - How to apply migrations (dotnet ef database update).
   - How to run web: dotnet run --project ZeroDawn.Web.
   - How to run MAUI Android: Visual Studio → select Android emulator → F5.
   - How to run MAUI Windows: Visual Studio → select Windows Machine → F5.
   - Default credentials: admin@zerodawn.local / Admin@123.
   - Known platform-specific API URL configuration.
   - Troubleshooting section: common MAUI errors, HTTPS cert issues on Android.

2. ARCHITECTURE.md:
   - Solution diagram (text-based).
   - Project responsibilities (from implementation_plan).
   - Folder structure for each project.
   - Data flow: Client → HttpClient (typed) → API Controller → Service → DB.
   - Auth flow: Login → JWT + Refresh → stored in SecureStorage/localStorage.
   - Error flow: Exception → Middleware → DB log → ApiResponse with reference number.
   - What is shared vs server-only.

3. ENVIRONMENT.md:
   - Configuration sources: appsettings.json → appsettings.{env}.json → User Secrets → env vars.
   - Table of all config keys, where they come from, which are secrets.
   - API URL strategy per platform/environment.
   - How to add a new config key.

4. AI_HANDOFF.md:
   - Project context for AI coding assistants.
   - Technology stack.
   - Key files and where to find them.
   - Coding conventions (file-scoped namespaces, nullable enabled, DataAnnotations for validation).
   - Common tasks with step-by-step instructions:
     - How to add a new page.
     - How to add a new API endpoint.
     - How to add a new DTO.
     - How to add a new shared component.
     - How to add a new localization key.
   - What NOT to do (secrets in client, controllers in shared, new HttpClient(), retry POST).

5. PUBLISHING.md:
   - Web deployment (dotnet publish, IIS/Azure App Service basics).
   - MAUI Android APK/AAB build.
   - MAUI Windows MSIX build.
   - Environment variable setup for production.
   - Database migration in production.
   - Checklist before publish.

Rules:
- Clear, actionable, no vague advice.
- Include exact commands where applicable.
- Reference actual file paths from the solution.
- Report: files created, word counts.
```

**Expected output**: 5 documentation files.
**Acceptance**: A new developer can read RUNNING.md and get the project running in under 15 minutes.

---

## 6 · Missing but Important

These items were not in your original request but are essential for a production starter:

| # | Item | Why | Recommendation |
|---|------|-----|----------------|
| 1 | **HTTPS certificate for MAUI Android** | Android emulator rejects self-signed certs by default. | Add a `DevHttpsConnectionFilter` or use the dev cert trust approach. Document in RUNNING.md. |
| 2 | **CORS policy** | WASM client runs on a different origin during development. | Add `builder.Services.AddCors()` with a named policy allowing the dev origin. Server-only. |
| 3 | **Anti-forgery handling** | Blazor WebAssembly and MAUI do not send anti-forgery tokens for API calls. | Use `[IgnoreAntiforgeryToken]` on API controllers since auth is JWT-based. Antiforgery is for cookie auth only. |
| 4 | **Health check endpoint** | Load balancers and monitoring need it. | Add `app.MapHealthChecks("/health")` — 2 lines of code. |
| 5 | **Request/response compression** | WASM downloads are large. | `app.UseResponseCompression()` with Brotli. Already built-in. |
| 6 | **Logout on all devices** | User changes password but old refresh tokens on other devices still work. | On password change, invalidate all refresh tokens for that user. |
| 7 | **Account lockout** | Brute force protection beyond rate limiting. | Enable Identity lockout: `options.Lockout.MaxFailedAccessAttempts = 5; options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);` |
| 8 | **Swagger/OpenAPI** | API documentation and testing during development. | Add `builder.Services.AddOpenApi()` + `app.MapOpenApi()` (built-in .NET 10). Dev-only. |
| 9 | **`.gitignore` updates** | User Secrets, bin/obj, .vs should be ignored. | Verify `.gitignore` covers all generated files. |
| 10 | **Version/build info endpoint** | Useful for debugging deployed versions. | `[GET] /api/info` returning assembly version + environment name. |

---

## 7 · Final Recommended Execution Order

```
                            ┌──────────────────┐
                            │   Phase 0        │
                            │   Foundation     │
                            └────────┬─────────┘
                    ┌────────────────┼────────────────────┐
                    │                │                     │
              ┌─────▼─────┐   ┌─────▼─────┐        ┌─────▼─────┐
              │  Phase 9   │   │  Phase 10  │        │  Phase 11  │
              │ Validation │   │ Localiz.   │        │ UI Comps   │
              └────────────┘   └────────────┘        └─────┬──────┘
                                                           │
                            ┌──────────────────┐           │
                            │   Phase 1        │           │
                            │   Config         │           │
                            └────────┬─────────┘           │
                    ┌────────────────┼──────────┐          │
                    │                │          │          │
              ┌─────▼─────┐  ┌──────▼────┐     │          │
              │  Phase 5   │  │  Phase 2   │     │          │
              │ Logging    │  │  DB/Ident. │     │          │
              └────────────┘  └──────┬─────┘     │          │
                                     │           │          │
                              ┌──────▼─────┐     │          │
                              │  Phase 3   │     │          │
                              │  Auth API  │     │          │
                              └──────┬─────┘     │          │
                              ┌──────▼─────┐     │          │
                              │  Phase 6   │     │          │
                              │  SMTP      │     │          │
                              └──────┬─────┘     │          │
                              ┌──────▼─────┐     │          │
                              │  Phase 7   │     │          │
                              │  Rate Lim. │     │          │
                              └──────┬─────┘     │          │
                              ┌──────▼─────┐     │          │
                              │  Phase 4   │     │          │
                              │  Client    │     │          │
                              └──────┬─────┘     │          │
                    ┌────────────────┼────────────┤          │
                    │                │            │          │
              ┌─────▼─────┐  ┌──────▼────┐ ┌─────▼──────────▼┐
              │  Phase 8   │  │ Phase 14   │ │   Phase 12     │
              │ Resilience │  │ MAUI Store │ │   Auth Pages   │
              └────────────┘  └────────────┘ └───────┬────────┘
                                                     │
                                              ┌──────▼─────┐
                                              │  Phase 13  │
                                              │  Dashboards│
                                              └──────┬─────┘
                                              ┌──────▼─────┐
                                              │  Phase 15  │
                                              │  Docs      │
                                              └────────────┘
```

### Linear execution order (if doing sequentially):

| Step | Phase | Est. Time |
|------|-------|-----------|
| 1 | Phase 0 — Foundation & Core | 1–2 hours |
| 2 | Phase 1 — Configuration | 1 hour |
| 3 | Phase 9 — Validation | 30 min |
| 4 | Phase 10 — Localization | 1 hour |
| 5 | Phase 2 — Database & Identity | 2 hours |
| 6 | Phase 5 — Logging & Error Handling | 2–3 hours |
| 7 | Phase 3 — Auth API & JWT | 3–4 hours |
| 8 | Phase 6 — SMTP & Email | 1–2 hours |
| 9 | Phase 7 — Rate Limiting | 30 min |
| 10 | Phase 4 — Client Auth Infrastructure | 3–4 hours |
| 11 | Phase 8 — API Resilience | 1 hour |
| 12 | Phase 11 — Shared UI Components | 4–6 hours |
| 13 | Phase 12 — Auth Pages | 3–4 hours |
| 14 | Phase 13 — Dashboard & Admin Pages | 4–6 hours |
| 15 | Phase 14 — MAUI Storage & Connectivity | 2 hours |
| 16 | Phase 15 — Documentation | 2–3 hours |

**Total estimated**: 30–45 hours of implementation work.

> [!TIP]
> Phases 9, 10, 11 can run in parallel with Phases 2–4 since they have no database dependency. This can save 5–8 hours.
