# Product Requirements for Jamaa

## General Description

Jamaa is a cross-platform Desktop and Mobile Application which simplifies the daily management of small associations or groups of people.
The name "Jamaa" is derived from the Swahili word for extended family, "community" or "group," reflecting the application's focus on serving the needs of small associations and groups.

  It handles different types of operations ranging from member registration and on-boarding to
  bookkeeping, financial reporting, preparation of tax declaration documents, setting up appointments and sending reminders and enabling
  easy communication between members.

  Jamaa serves the administrative and leadership members to record, plan, organize, decide, report and communicate
  group activities, transactions, events and decisions.
  For the other group members, it serves for communication, to inform themselves about group events and
  get up-to-date and accurate individual and group financial and other reports.
  
## Functional Requirements

### Initial Setup
  - On first launch of the application, the system checks if an organization and a superuser have been created. If not, a wizard is presented to guide the user through the setup process.

### User Management
   - User Accounts:
     - CRUD (create, read, update, delete)
     - print (lists and selected)
     - edit credentials (username/password)
     - add to user group
     - remove from user role
     - Login
     - Logout
   - User Role:
     - CRUD
     - grant permissions
     - revoke permissions
     - print
   - User Role Permissions
     - CRUD
     
### Member Management
  - Register
  - Unregister
  
  - View member profile
  - Print member profile
  - Send message
  - view member list
  - print member list

### User-Group Management
  - CRUD
  - Add member
  - Remove member
  - print user-group
  
### Event Management (recurrent events can have several instances)
  - CRUD
  - Print list for selected period
  - View and print attendance (if event has taken place)
  - Record attendance [for selected member(s)]
  
### Fees Management
  - CRUD
  - Bill member/group
  - Cancel member billing
  - Record payments (audit trail)
  - Edit payments (audit trail)
  - List payments from selected member

### Financial Accounting (audit trails)
  - CRUD accounts
  - CRUD ledgers
  - Record transactions
  - List transactions
  - Generate financial reports
  - List financial reports
  - Print financial reports