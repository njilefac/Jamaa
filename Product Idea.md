# Product Requirements for Club Station

## General Description

  Club Station is a cross-platform Desktop and Mobile Application which simplifies the daily management
  of small associations or groups of people.

  It handles different types of operations ranging from member registration and on-boarding to
  financial recording and reporting, setting up appointments and sending reminders and enabling
  easy communication between members.

  Club Station serves the administrative and leadership members to record, plan, organize, decide, report and communicate
  group activities, transactions, events and decisions.
  For the other group members, it serves for communication, to inform themselves about group events and
  get up-to-date and accurate individual and group financial and other reports.
  
## Functional Requirements
### User Management
   - User Accounts:
     - CRUD (create, read, update, delete)
     - print (lists and selected)
     - edit credentials (username/password)
     - add to user group
     - remove from user group
     - Login
     - Logout
   - User Groups:
     - CRUD
     - grant permissions
     - revoke permissions
     - print
   - User Group Permissions
     - CRUD
     
### Member Management
  - Register
  - Unregister
  - Assign fee
  - Un-assign fee
  - View member profile
  - Print member profile
  - Send message
  - view member list
  - print member list

### Sub-Group Management
  - CRUD
  - Add member
  - Remove member
  - print sub-group
  
### Event Management (recurrent events can have several instances)
  - CRUD
  - Print list for selected period
  - View and print attendance (if event has taken place)
  - Record attendance [for selected member(s)]
  
### Fees Management
  - CRUD
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