Feature "User Authentication":
Scenarios "Login Screen on App Launch":

Scenario "login screen on first launch":
    Given a user "Joe"
    When they launch the app
    Then they should see the login screen
    And the credential fields should be empty
    And the login button should be disabled
    And only the exit menu should be enabled