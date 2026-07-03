# Board Game Picker

A full-stack app to randomly select a board game from your collection based on filters like player count, age range, and game type.

## Stack
- **Backend**: ASP.NET Core 8 Web API + Entity Framework Core + PostgreSQL
- **Frontend**: React + TypeScript + Vite

## Getting Started

### Prerequisites
- .NET 8 SDK
- Node 18+
- PostgreSQL running locally

### Backend

1. Update the connection string in `backend/BoardGamePicker.API/appsettings.json` if needed.
2. Run migrations and seed data automatically on first start:

```bash
cd backend/BoardGamePicker.API
dotnet run
```

The API runs on `http://localhost:5000`. Swagger UI is available at `/swagger`.

### Frontend

```bash
cd frontend
npm install
npm run dev
```

The app runs on `http://localhost:5173`.

## Features
- Filter games by player count, age range, max runtime, type, and category
- Pick a random game from matching results
- Browse all matching games in a list
- 100 seeded games from the BGG top 100

## Data Model

| Field | Type | Description |
|---|---|---|
| Name | string | Game title |
| MinPlayers / MaxPlayers | int | Player count range |
| MinRuntime / MaxRuntime | int | Duration in minutes |
| MinAge | int | Minimum recommended age |
| ImageUrl | string? | Cover art URL |
| Description | string | Short description |
| Type | string | e.g. Strategy, Co-op, Party, Family |
| Category | string | e.g. Deckbuilding, Worker Placement |
| BggRank | int | BoardGameGeek ranking |
| IsOwned | bool | Whether you own this game |
