# CMCS - Claims Management and Verification System 

## 📌 Overview

The **Claims Management and Verification System** (CMCS Portal) is an ASP.NET Core MVC web application developed as part of the PROG6212 Portfolio of Evidence (PoE).
It streamlines the process of submitting and approving monthly claims for Independent Contractor (IC) lecturers by simulating a real-world workflow. The system provides:
Lecturers with a digital platform to submit teaching claims and upload supporting documents.
Programme Coordinators with tools to verify claims.
Programme Managers with functionality to approve or reject claims. HR Managers with reporting and export features for financial oversight.
This project demonstrates skills in **C#**, **ASP.NET Core MVC**, **GUI design**, **database integration**,and **project planning**, aligning with module outcomes for Programming 2B.

---

## 🚀 Features (Prototype Implementation)
- **Lecturer Features**
  - Submit monthly claims (mock navigation only, no persistence).
  - Upload supporting documents (to be implemented in later iterations).
  - Track claim statuses with sample data provided.
  - Lecturer claim submission with document uploads.
  - Submit monthly claims with real-time total calculation.
  - Upload supporting documents (PDF, DOCX, XLSX with validation).
  - Track claim statuses (Pending, Verified, Approved, Rejected).
  - Auto-approval for low-value claims (≤ R1000).

- **Programme Coordinator Features**
  - View lecturer-submitted claims.
  - Verify accuracy of submitted claims.
  - Pre-approve pending claims (sample data available).
  - Coordinator and Manager claim review workflow.
  - View lecturer-submitted claims.
  - Verify accuracy of submitted claims.
  - Pre-approve pending claims with anomaly detection (e.g., high hourly rates).

- **Programme Manager Features**
  - View pre-approved claims from coordinators.
  - Approve or reject claims (sample data available).
  - Coordinator and Manager claim review workflow.
  - View pre-approved claims from coordinators.
  - Approve or reject claims with audit trail logging.
  
- **HR Manager Features**
  - Dashboard to view approved claims.
  - Export reports in CSV and PDF invoice format.
  - Subtotals per lecturer and grand totals for all claims.
  
- **Shared Features**
  - Navigation bar with role-based actions.
  - Responsive and user-friendly interface with a cyan + gray theme.
  - Sample data tables for Track, Pre-Approve, and Approve views.

- **Additional Features**
  - Real-time claim status updates (Pending, Verified, Approved, Rejected)
  - LocalDB auto-initialization
  - xUnit unit tests for core functionalities
  - Responsive dark-themed UI
---

## System Structure
**Code**
CMCS_POE_PART_2/
├── Controllers/
│   ├── AccountController.cs   # Login, Register, Logout with hashing
│   ├── ApprovalController.cs  # Verify and Approve claims
│   ├── ClaimsController.cs    # New claim submission, Index for tracking
│   ├── HomeController.cs      # Role-based dashboard
│	└── HRController.cs        # View claims and manage 
│
├── Models/
│   ├── Claim.cs               # Claim model
│   ├── DbHelper.cs               # DbHelper model
│	├── ErrorViewModel.cs               # ErrorView model
│   └── User.cs                # User model
│
├── Reports/
│   └── ApprovedClaimsReport.cs           # Claim Report or Invoice Pdf
│
├── Data/
│   └── auto_create_instance_db_tables.cs           # Database creator and manager (CRUD for Users/Claims)
│
├── Views/
│   ├── Account/               # Login, Register
│   │   ├── Login.cshtml   # Login razor
│	│	└── Register.cshtml        # Register razor
│   │
│   ├── Approval/              # Approve, Verify
│   │   ├── Approve.cshtml   # Approve razor
│	│	└── Verify.cshtml        # Verify razor
│   │
│   ├── Claims/                # Index (track), New
│   │   ├── Index.cshtml   # Track razor
│	│	└── New.cshtml        # Claim maker razor
│   │
│   ├── Home/                  # Index (dashboard)
│	│	└── Index.cshtml        # Dashboard razor
│	│
│   └── Shared/                # _Layout (navbar, alerts)
│		└── _Layout.cshtml        # Layout razor
│	
├── wwwroot/
│   ├── css/site.css           # Custom theme (Red + Gray, dark mode)
│   ├── images/cmcs-logo.png           # Logo Image
│   └── js/app.js              # Form handling, animations
│
├── Program.cs                 # App setup, session, routes
│
│
├── appsettings.json                 # Database Connection
│
├── Test.cs  # Test project Important changed for part 3
│
└── XUnitText.cs                # Test project xunit test

---

## Technology Stack
- **Framework:** ASP.NET Core MVC (.NET 8.0)
- **Language:** C#  
- **Frontend:** Razor Views, HTML5, CSS3, Bootstrap 5, Animate.css, AOS.js
- **Database:** SQL LocalDB (auto-created via script)
- **Frontend:** Bootstrap 5, AOS.js, Animate.css
- **Testing:** xUnit + Moq 
- **Styling:** Custom CSS (cyan + Gray theme, responsive design)  
- **IDE:** Visual Studio 2026 (or later)  

---

## Assumptions and Constraints
- Authentication and data persistence are **not implemented** in this prototype.  
- Sample data is used to simulate system workflows.  
- Users (lecturers, coordinators, managers) are assumed to already have valid credentials.  
- Internet access and modern browsers are required to use the system.  

---

## Setup Instructions
1. Clone the repository or copy the project files into Visual Studio.
2. Ensure you have **.NET 6 SDK** (or higher) installed.
3. Open the solution in Visual Studio.
4. Run the project (`F5`).  
   - The application starts on the **Login page**.  
   - Use the navigation bar to move between **Home**, **Claim**, **Track Claim**, **Pre-Approve**, and **Approve**.  
5. Access the site at `https://localhost:5001` (or your configured port).
6. Workflow:
   - Register → Login → Redirected to role-based dashboard (Lecturer, Coordinator, Manager, HR Manager).
7. When Project Runs:
	- It will start the user on login page click on register button to go there
	- Then on register enter details and pick a role 
	- Then it will take you back to login where when logining in will take you to **Lecturer**, **Program Coordinator**, **Program Manager**, **HR Manager** pages
---

## Database Initialization
The system automatically creates a LocalDB instance (`claim_system`) and required tables (`Users`, `Claims`). This process runs synchronously at startup to ensure reliability.

## Testing
To run unit tests:
```bash
dotnet test
```
xUnit tests mock the `IDbHelper` interface to ensure controllers behave correctly without hitting a real database.

---

## Project Plan (Prototype Timeline)
- **Week 1:** Gather requirements and design layout.  
- **Week 2:** Build Login and navigation system.  
- **Week 3:** Add role-based Home page cards.  
- **Week 4:** Implement Track Claim, Pre-Approve, and Approve pages with sample data.  
- **Week 5:** Apply red + gray theme styling and refine UI.  
- **Week 6:** Documentation and submission preparation.  

---

## Version Control Practices
- Frequent commits for each feature addition (e.g., *"Added TrackClaim sample table"*).  
- Descriptive commit messages.  
- Git used for version control (can be integrated with GitHub). 
- Maintain **at least 5 commits** with descriptive messages:
  1. `feat: implement IDbHelper and DI setup`
  2. `fix: corrected DbHelper and SqlDataReader logic`
  3. `chore: added xUnit tests for ClaimsController`
  4. `style: UI cleanup and dark mode polish`
  5. `docs: updated README and project documentation` 

---

## Future Improvements
- Implement real authentication and role-based authorization.  
- Connect to a database for storing and retrieving claims.  
- Add full document upload functionality with validation.  
- Improve error handling with user-friendly feedback.  
- Expand reporting features for managers.  

---

## Author
- Student: Sibongiseni Collel Ngwamba
- Student Number: ST10454956
- Module: **PROG6212 – Programming 2B Portfolio of Evidence**
- Year: 2025
- Institution: **The Independent Institute of Education (IIE)**

## GitHub Link
- Link: https://github.com/sibongiseni-ngwamba/CMCS_POE_Part1.git
- Link Part 2 AND 3: https://github.com/sibongiseni-ngwamba/CMCS_POE_PART_2.git

