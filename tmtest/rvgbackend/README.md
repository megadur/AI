# Assessment & Document Management Backend

This is a .NET 8 Web API backend for managing assessments, PDF documents, and messaging. It supports JWT authentication, role-based access, and is designed to integrate with an Angular frontend.

## Features
- User authentication (JWT)
- Assessment assignment management
- PDF document management (list, preview, deny download)
- Messaging system (by assessment, by document, general)
- Extensible for real-time chat and PDF signing

## Project Structure
- Controllers: API endpoints
- Models: Entity models
- DTOs: Data transfer objects
- Services: Business logic
- Repositories: Data access

## Getting Started
1. Ensure you have .NET 8 SDK installed.
2. Restore dependencies:
   ```
   dotnet restore
   ```
3. Build the project:
   ```
   dotnet build
   ```
4. Run the project:
   ```
   dotnet run
   ```

## Next Steps
- Implement database context and migrations
- Add authentication and authorization
- Build out controllers and services per the PRD

See the PRD and technical specifications in the workspace for details.
