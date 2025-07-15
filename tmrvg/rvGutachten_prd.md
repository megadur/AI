<context>

# Overview  

rvGutachten is a web-based platform designed for assessors to efficiently manage, read, and track assessment assignments. The application enables assessors to browse, view, and interact with a list of PDF documents associated with each assignment, as well as send and read messages related to those documents or general communications. The platform streamlines document access and communication, improving productivity and knowledge sharing among assessors. Assignment management and PDF uploads are handled by a separate administrative app, allowing rvGutachten to focus on the assessor experience.

# Core Features  

- **Assignment Listing & Tracking**: Assessors can view a list of their assigned assessments, track their status, and mark them as read or accepted.
- **PDF Document Browsing & Viewing**: For each assignment, assessors can browse and view associated PDF documents in-browser, with support for multiple documents per assignment.
- **Assesment upload**: Assessors can upload assesments as pDF and sign them.
- **Document-Related Messaging**: Assessors can send and receive messages linked to assigned assessments, facilitating discussion and clarification.
- **General Messaging**: Assessors can also send and receive messages not tied to a specific assessments, supporting broader communication.
- **Notifications**: Real-time or periodic notifications for new assignments, messages, or document updates.
- **User Authentication**: Secure login for assessors, with role-based access control.

# User Experience  

- **User Personas**: Primary users are assessors who need to efficiently process and communicate about assessment assignments and documents.
- **Manager Personas**: Managers that create assessment assignments and add specific PDF documents. They can also communicate with assesors
- **Admin Personas**: Admins manage assessorsa and co-workers. They can create update list and delete them.
- **Key User Flows**:
  - Login and view dashboard of assignments
  - Select an assignment to view details and associated PDFs
  - Open a PDF document and read or annotate (if supported)
  - Send/read messages related to a document or assignment
  - Receive notifications for new assignments or messages
- **UI/UX Considerations**:
  - Clean, intuitive interface focused on document and message access
  - Responsive design for desktop and tablet use
  - Clear separation between document-related and general messages

</context>

<PRD>

# Technical Architecture  

- **System Components**:
  - Frontend web app for assessors (rvGutachten.Web)
  - Frontend web app for managers (rvGutachten.Verw)
  - Frontend web app for admins (rvGutachten.Admin)
  - Backend API for assignment, document, and messaging data
  - Integration with a separate admin/management app for assignment and PDF uploads
  - Spring-Boot Web API (backend)
  - Angular SPA (frontend)
  - Database (PostgreSQL)
  - File storage (local or cloud, e.g., Azure Blob Storage)
- **Data Models**:
  - Assignment: id, title, status, assessorId, documentIds, etc.
  - Document: id, assignmentId, fileUrl, metadata
  - Message: id, senderId, recipientId, assignmentId, documentId (optional), content, timestamp
  - User: id, name, role, contact info
- **APIs and Integrations**:
  - REST API for assignments, documents, and messages
  - Authentication API (JWT or OAuth2)
  - WebSocket or polling for real-time notifications
- **Infrastructure Requirements**:
  - Cloud hosting for frontend and backend
  - Secure storage for PDF documents
  - Database for assignments, users, messages

# Development Roadmap  

- **MVP Requirements**:
  - User authentication and role management
  - Assignment listing and status tracking
  - PDF document browsing and viewing
  - Messaging (document-related and general)
  - Basic notifications
- **Future Enhancements**:
  - PDF annotation tools
  - Advanced search and filtering
  - Mobile app version
  - Integration with calendar or task management tools
  - Analytics/dashboard for assessors
  - Analytics/dashboard for managers

# Logical Dependency Chain

- Implement user authentication and basic user model first
- Build assignment listing and tracking (requires user model)
- Add PDF document browsing/viewing (requires assignments)
- Implement messaging (requires assignments and documents)
- Add notifications (requires messaging and assignments)
- Integrate with admin/management app for assignment/document sync
- Enhance with future features as needed

# Risks and Mitigations  

- **Technical challenges**: PDF rendering and annotation in-browser; mitigate by using proven libraries (e.g., PDF.js)
- **MVP scoping**: Risk of feature creep; mitigate by strict MVP/future split
- **Resource constraints**: Limited dev resources; mitigate by focusing on core flows first
- **Integration**: Ensuring smooth sync with admin app; mitigate by defining clear API contracts

# Appendix  

- Research: Review of PDF.js and messaging frameworks
- Technical specs: API endpoints, data model diagrams (to be detailed in technical docs)
</PRD>
