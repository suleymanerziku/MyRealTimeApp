# Real-Time Document Collaboration Suite

A modern web application that enables real-time collaborative document editing with automatic persistence and version control. Multiple users can edit the same document simultaneously, with changes synchronized instantly across all connected clients.

---

## Project Overview

### What This Project Does

This application provides:
- **Real-Time Collaboration**: Multiple users can view and edit the same document simultaneously with instant updates via SignalR
- **Automatic Autosave**: Document changes are automatically saved to the database after 3 seconds of inactivity
- **Local Draft Persistence**: Drafts are stored in browser IndexedDB, preserving work even if the connection is lost
- **Document Management**: Browse, open, and manage multiple documents from a sidebar interface
- **Version Control**: Each document maintains a version number that increments with every save to detect conflicts
- **Conflict Detection**: Version mismatch alerts prevent accidental data loss from concurrent edits

### How It Works

1. **Frontend (Sidebar + Editor)**
   - Users select documents from a left sidebar list
   - Selected document content loads into the editor textarea
   - Sidebar shows available documents with ID, version, and text snippet

2. **Autosave & Local Storage**
   - As users type, changes are debounced and stored locally in IndexedDB
   - After 3 seconds of inactivity, the editor content is posted to `/api/documents/autosave`
   - The API creates new documents or updates existing ones with version increments

3. **Real-Time Collaboration via SignalR**
   - When a user opens a document, the frontend joins a document-specific SignalR group
   - When a user saves, the API broadcasts the update to all users in that document's group
   - Other viewers immediately receive and display the updated text and version

4. **Backend Architecture**
   - ASP.NET Core minimal API handles document CRUD and autosave endpoints
   - PostgreSQL database stores documents with EF Core ORM
   - SignalR hub manages real-time group communication
   - Automatic database migrations on startup

---

## Architecture & Key Concepts

### Frontend Flow

```
User Types
  ↓
Input Event (isLocalChange = true)
  ↓
Debounce Timer (3 seconds)
  ↓
POST /api/documents/autosave
  ↓
Server responds with updated ID & version
  ↓
Update metadata (docId, version)
  ↓
Broadcast via SignalR to document group
  ↓
Other Viewers Receive Update
  ↓
Apply update (if document ID matches & isLocalChange = false)
  ↓
Display new text & version
```

### Backend Flow

```
POST /api/documents/autosave (with id, text, version)
  ↓
Check if document exists
  ├─ Yes: Verify version matches → increment version → update text
  └─ No: Create new document with version = 1
  ↓
Save to PostgreSQL
  ↓
Broadcast "ReceiveDocumentUpdate" to document group via SignalR
  ↓
Return updated document (ID, version)
```

### Key Components

#### 1. **AppDbContext** (`src/Infrastructure/Data/AppDbContext.cs`)
- EF Core DbContext for PostgreSQL
- Manages `Documents` table
- Automatically migrated on application startup

#### 2. **Document Entity** (`src/Domain/Entities/Document.cs`)
- ID: Unique identifier
- Text: Document content
- Version: Incremented on each save
- UpdatedAt: Timestamp of last update

#### 3. **NotificationHub** (`src/Infrastructure/Hubs/NotificationHub.cs`)
- SignalR hub for real-time communication
- `JoinDocument(int documentId)`: User joins a document room
- `LeaveDocument(int documentId)`: User leaves a document room
- `BroadcastDocumentUpdate(int documentId, string text, int version)`: Sends update to all users in the room

#### 4. **API Endpoints** (`src/Api/Program.cs`)
- `POST /api/documents/autosave`: Save or create document with conflict detection
- `GET /api/documents/{id}`: Fetch a single document
- `GET /api/documents`: List all documents ordered by most recently updated

#### 5. **Frontend JavaScript** (`index.html`)
- IndexedDB integration for local draft persistence
- SignalR connection management with automatic reconnect
- Document list sidebar with real-time updates
- Editor with input debouncing and conflict handling

---

## Setup & Installation

### Prerequisites

- **.NET 10.0 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **PostgreSQL 12+**: [Download](https://www.postgresql.org/download/)
- **Node.js/npm** (optional, for frontend tooling): [Download](https://nodejs.org/)

### Step 1: Clone & Navigate

```bash
cd "c:\Users\suley\OneDrive\Documents\HST\Audit Suite\MyRealTimeApp"
```

### Step 2: Configure PostgreSQL Connection

Edit `src/Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Port=5432;Database=realtime_collab;User Id=postgres;Password=your_password;"
  }
}
```

Replace:
- `localhost` with your PostgreSQL server (use `127.0.0.1` if needed)
- `5432` with your PostgreSQL port (default: 5432)
- `realtime_collab` with desired database name
- `postgres` with your PostgreSQL user
- `your_password` with your PostgreSQL password

**Note**: Ensure the database exists or the application will attempt to create it during migration.

### Step 3: Restore & Build

```bash
dotnet restore
dotnet build
```

Expected output:
```
  Domain net10.0 succeeded
  Application net10.0 succeeded
  Infrastructure net10.0 succeeded
  Api net10.0 succeeded
Build succeeded!
```

### Step 4: Run the Application

```bash
dotnet run --project src/Api/Api.csproj
```

Expected output:
```
Now listening on: http://localhost:5115
Application started. Press Ctrl+C to exit.
```

### Step 5: Access the Application

Open your browser and navigate to:

```
http://localhost:5115
```

You should see:
- A left sidebar with "Available Documents" and a Refresh button
- A main editor area with status indicators
- A SignalR connection status badge

---

## Usage Guide

### Creating a New Document

1. Click on the editor textarea (bottom right)
2. Start typing your document content
3. After 3 seconds of inactivity, the document is automatically saved
4. A new Document ID will be assigned and displayed in the metadata

### Opening an Existing Document

1. Click the **Refresh** button in the sidebar to fetch the latest document list
2. Click on any document item in the sidebar (e.g., `#1 · v2`)
3. The document opens in the editor
4. The document's ID and version are displayed in the metadata
5. The item is highlighted with a blue border to indicate selection

### Real-Time Collaboration

1. Open the same document in two browser windows/tabs
2. Edit the document in one window
3. After the autosave timer (3 seconds), the changes appear instantly in the other window
4. The status bar shows: `Updated by collaborator (v1 → v2)`

### Handling Version Conflicts

If you edit a document offline and another user edits the same document:
- A conflict alert appears: `Save conflict: version mismatch. Refresh to reconcile.`
- The local IndexedDB draft is preserved
- You can refresh the page to reload the latest version and retry

### Local Draft Recovery

- If your browser closes unexpectedly, your draft is stored in IndexedDB
- On restart, the draft is restored with a message: `Draft restored. Continue editing to autosave.`
- Resume editing or refresh to load the latest server version

---

## Project Structure

```
MyRealTimeApp/
├── index.html                          # Main UI (sidebar + editor + SignalR client)
├── MyRealTimeApp.slnx                 # Solution file
└── src/
    ├── Api/                            # ASP.NET Core API
    │   ├── Program.cs                 # Startup, endpoints, migrations
    │   ├── Api.csproj
    │   ├── appsettings.json           # Configuration
    │   └── appsettings.Development.json
    ├── Application/                    # Application layer (services)
    │   ├── Application.csproj
    │   └── Services/
    │       └── OrderService.cs
    ├── Domain/                         # Domain layer (entities, interfaces)
    │   ├── Domain.csproj
    │   ├── Entities/
    │   │   └── Document.cs            # Document entity
    │   └── Interfaces/
    │       └── INotificationService.cs
    └── Infrastructure/                 # Infrastructure layer (data, hubs, services)
        ├── Infrastructure.csproj
        ├── Data/
        │   └── AppDbContext.cs        # EF Core DbContext
        ├── Hubs/
        │   └── NotificationHub.cs     # SignalR hub
        └── Services/
            └── NotificationService.cs
```

---

## Technology Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Backend** | ASP.NET Core 10.0 | Web API framework |
| **Database** | PostgreSQL | Persistent document storage |
| **ORM** | Entity Framework Core 10.0 | Database abstraction |
| **Real-Time** | SignalR 7.0 | WebSocket-based messaging |
| **Frontend** | HTML5, CSS3, JavaScript | UI & client-side logic |
| **Local Storage** | IndexedDB | Browser-based draft persistence |

---

## Troubleshooting

### Build Fails with EF Expression Tree Error

**Error**: `CS8790: An expression tree may not contain a pattern System.Index or System.Range indexer access`

**Solution**: EF Core cannot translate C# range syntax (`d.Text[..120]`). Use `Substring()` instead:
```csharp
d.Text.Substring(0, 120)
```

### SignalR Connection Fails

**Error**: `SignalR connection failed: WebSocket connection to 'ws://...' failed`

**Solution**:
1. Verify the API is running: `dotnet run`
2. Check CORS policy in `Program.cs`:
   ```csharp
   policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials()
   ```
3. Ensure SignalR hub is mapped: `app.MapHub<NotificationHub>("/notificationHub");`

### Database Connection Fails

**Error**: `NpgsqlException: could not translate host name "localhost" to address`

**Solution**:
1. Verify PostgreSQL is running: `pg_isready`
2. Update connection string in `appsettings.json`
3. Test with `psql` client:
   ```bash
   psql -U postgres -h localhost -d realtime_collab
   ```

### Updates Not Appearing for Other Users

**Solution**:
1. Verify both users have successfully joined the document room (check browser console for errors)
2. Confirm SignalR connection status shows "Connected" (green badge)
3. Refresh the document list to ensure correct document ID
4. Check that the server broadcast is sending to the correct group: `document-{documentId}`

---

## Development Workflow

### Making Backend Changes

1. Edit files in `src/` directories
2. Run `dotnet build` to compile
3. Run `dotnet run --project src/Api/Api.csproj` to start

### Adding Database Migrations

```bash
dotnet ef migrations add YourMigrationName --project src/Infrastructure -s src/Api
dotnet ef database update --project src/Infrastructure -s src/Api
```

### Making Frontend Changes

1. Edit `index.html`
2. Refresh browser (`F5` or `Ctrl+R`)
3. Clear IndexedDB if draft conflicts occur (DevTools → Application → IndexedDB → autosave-db → Delete)

---

## Future Enhancements

- User authentication and authorization
- Document-level permissions
- Rich text editing (Markdown support)
- Version history and rollback
- Document tagging and search
- Real-time cursor position tracking
- Operational transformation for conflict-free merging
- Document export (PDF, Docx)
- Notification system for document mentions

---

## License

This project is part of the HST Audit Suite and is provided as-is for internal use.

---

## Support

For issues or questions:
1. Check the **Troubleshooting** section above
2. Review browser console logs (F12 → Console)
3. Check server logs in the terminal where `dotnet run` is executing
4. Verify database connectivity: `psql -U postgres -h localhost`

