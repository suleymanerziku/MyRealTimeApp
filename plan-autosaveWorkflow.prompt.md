## Plan: IndexedDB autosave workflow with Postgres backend

TL;DR: Replace the current SignalR notification demo with a form that saves drafts in IndexedDB, debounces edits for 3 seconds, and sends a background autosave request to a new API endpoint that persists the document and version to Postgres.

Steps
1. Update the domain model
   - Modify `src/Domain/Entities/Document.cs`
   - Add a `Version` int property and optional `UpdatedAt` timestamp to support versioned autosave.

2. Add a background save API endpoint
   - Modify `src/Api/Program.cs`
   - Add a POST endpoint at `/api/documents/autosave` that accepts an autosave request payload.
   - The endpoint should:
     - load or create the `Document` entity
     - compare incoming `Version` with stored `Version`
     - if the request is stale, return a conflict or current version response
     - update `Text`, increment `Version`, save to the database
     - return the new `Id` and `Version`
   - Optionally add `GET /api/documents/{id}` for initial load.

3. Replace the test page with an autosave form
   - Modify `index.html`
   - Replace the existing notification UI with:
     - a textarea for document text
     - a status display for "Saved", "Saving...", "Error", "Draft saved"
     - optional save indicator UI and current version display
   - Implement client logic in JS:
     - open an IndexedDB database like `autosave-db` and object store `drafts`
     - on every edit, mark the document dirty and save the draft immediately to IndexedDB
     - reset a 3-second debounce timer on each keystroke
     - when debounce fires, send a background autosave request to the API
     - include `Id`, `Text`, and `Version` in the request body
     - on success, clear dirty state, remove the draft from IndexedDB, update the version, and show "Saved"
     - on failure, keep the draft, show an error, and optionally retry later
     - restore any saved draft from IndexedDB on page load

4. Handle schema changes and migrations
   - If `Version` is new, add a migration in `src/Infrastructure` and update the database
   - Keep `db.Database.Migrate()` in `src/Api/Program.cs` to apply migrations at startup during development

Relevant files
- `src/Domain/Entities/Document.cs` — add autosave metadata fields
- `src/Api/Program.cs` — add autosave endpoint and keep CORS / DbContext registration
- `index.html` — replace with autosave form, IndexedDB logic, debounce, and status updates
- `src/Infrastructure/Data/AppDbContext.cs` — already has `DbSet<Document> Documents`

Verification
1. Build with `dotnet build` and run `dotnet run --project src/Api/Api.csproj`
2. Serve `index.html` over HTTP and open it in browser
3. Type text into the editor, stop for 3 seconds, and verify a POST occurs to `/api/documents/autosave`
4. Confirm browser status shows "Saved" and the backend persisted a row in `Documents`
5. Confirm a draft is restored from IndexedDB after refresh if save has not completed
6. Simulate backend failure and verify the page shows an error and does not lose the draft

Decisions
- Use a minimal API endpoint instead of SignalR for autosave, because the workflow is request/response oriented.
- Save drafts in IndexedDB as an immediate client-side fallback before the 3-second autosave trigger.
- Keep `Version` as an integer for simple optimistic concurrency and UI version tracking.
