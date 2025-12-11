Feature: EHU Website End-to-End Tests
    As a user
    I want to navigate the EHU website
    So that I can access all its features

    Background:
        Given I have opened the web browser

    @Navigation
    Scenario: Navigate to About page
        Given I am on the EHU homepage
        When I click on the "About" link
        Then I should be redirected to the About page
        And the page title should contain "About"
        And the main header should be "About"

    @Search
    Scenario: Search for study programs
        Given I am on the EHU homepage
        When I search for "study programs"
        Then I should see search results for "study programs"

    @Language
    Scenario: Switch to Lithuanian language
        Given I am on the EHU homepage in English
        When I switch the language to Lithuanian
        Then I should be on the Lithuanian version of the website

    @Contact
    Scenario: View contact information
        Given I navigate to the contact page
        Then I should see all required contact information
        And I should see social media links

    @CompleteJourney @EndToEnd
    Scenario: Complete user journey
        Given I start on the EHU homepage
        When I navigate through all major sections
        Then I should have completed a full user journey