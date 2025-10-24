# CMCS - Claims Management and Verification System 

## Overview
The CMCS **Claims Management and Verification System** is an ASP.NET Core MVC web application developed as part of the PROG6212 Portfolio of Evidence (PoE). 
It was designed to streamline the process of submitting and approving monthly claims for Independent Contractor (IC) lecturers. 
By simulating a real-world workflow, the CMCS provides lecturers, programme coordinators, and managers with a simplified digital platform for handling claim submissions, pre-approvals, and approvals.
By simulating a real-world workflow, the CMCS provides lecturers, program coordinators, and managers with a simplified digital platform for handling claim submissions, pre-approvals, and approvals.
It allows lecturers to submit teaching claims, program coordinators to verify them, and academic managers to approve or reject them.
The system ensures transparent claim tracking and file upload for supporting documents.

This project demonstrates skills in **C#**, **ASP.NET Core MVC**, **GUI design**, and **project planning**, aligning with the module outcomes for Programming 2B.

---

## Features (Prototype Implementation)
- **Lecturer Features**
  - Submit monthly claims (mock navigation only, no persistence).
  - Upload supporting documents (to be implemented in later iterations).
  - Track claim statuses with sample data provided.
  - Lecturer claim submission with document uploads

- **Programme Coordinator Features**
  - Lecturer claims submission with document uploads

- **Program Coordinator Features**
  - View lecturer-submitted claims.
  - Verify accuracy of submitted claims.
  - Pre-approve pending claims (sample data available).
  - Coordinator and Manager claim review workflow

- **Programme Manager Features**
  - View pre-approved claims from coordinators.
  - Approve or reject claims (sample data available).
  - Coordinator and Manager claim review workflow

- **Shared Features**
  - Navigation bar with role-based actions.
  - Responsive and user-friendly interface with a red + gray theme.
  - Sample data tables for Track, Pre-Approve, and Approve views.

- **Additional Features**
  - Real-time claim status updates (Pending, Verified, Approved, Rejected)
  - LocalDB auto-initialization
  - xUnit unit tests for core functionalities
  - Responsive dark-themed UI
---

## System Structure
CMCS/
- CMCS/
├── Controllers/
│   ├── AccountController.cs   # Login, Register, Logout with hashing
│   ├── ApprovalController.cs  # Verify and Approve claims
│   ├── ClaimsController.cs    # New claim submission, Index for tracking
│   └── HomeController.cs      # Role-based dashboard
│
├── Models/
│   ├── Claim.cs               # Claim model
│   └── User.cs                # User model
│
├── Data/
│   └── DbHelper.cs            # Database operations (CRUD for Users/Claims)
│
├── Views/
│   ├── Account/               # Login, Register
│   ├── Approval/              # Approve, Verify
│   ├── Claims/                # Index (track), New
│   ├── Home/                  # Index (dashboard)
│   └── Shared/                # _Layout (navbar, alerts)
│
├── wwwroot/
│   ├── css/site.css           # Custom Grok-inspired theme
│   └── js/app.js              # Form handling, animations
│
├── Program.cs                 # App setup, session, routes
│
└── CMCS.Tests/                # Test project
    ├── AccountControllerTests.cs
    ├── ApprovalControllerTests.cs
    ├── ClaimsControllerTests.cs
    └── DbHelperIntegrationTests.cs  # With edge cases

---

## Technology Stack
- **Framework:** ASP.NET Core MVC (.NET 8.0)
- **Language:** C#  
- **Frontend:** Razor Views, HTML5, CSS3 
- **Database:** SQL LocalDB (auto-created via script)
- **Frontend:** Bootstrap 5, AOS.js, Animate.css
- **Testing:** xUnit + Moq 
- **Styling:** Custom CSS (Red + Gray theme, responsive design)  
- **IDE:** Visual Studio 2022 (or later)  

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
6. When Project Runs:
	- It will start the user on login page click on register button to go there
	- Then on register enter details and pick a role 
	- Then it will take you back to login where when logining in will take you to **Lecturer**, **Program Coordinator**, **Program Manager** pages
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
- **Week 2:** Build a Login and navigation system.  
- **Week 3:** Add role-based Home page cards.  
- **Week 4:** Implement Track Claim, Pre-Approve, and Approve pages with sample data.  
- **Week 5:** Apply red + gray theme styling and refine UI.  
- **Week 6:** Documentation and submission preparation.  

---

## Version Control Practices
- Frequent commits for each feature addition (e.g., *"Added TrackClaim sample table"*).  
- Descriptive commit messages.  
- Git used for version control (can be integrated with GitHub). 
- Git is used for version control (can be integrated with GitHub). 
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
- Link Part 2. I could not get the First Repo: https://github.com/sibongiseni-ngwamba/CMCS_POE_PART_1.git

