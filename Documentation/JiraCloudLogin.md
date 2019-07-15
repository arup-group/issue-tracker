# Jira Cloud Account Setup Guide
This is an important update since a new secured login method enforced by **Atlassian Jira Cloud** would take effect on 1st December 2018. **The current login method would stop working.** Please refer to the steps below to update your credentials. Note that this only affects Jira Cloud (https://arupdigital.atlassian.net or https://ovearup.atlassian.net). Our internal Jira Server (http://jira.arup.com) is working as usual. If you fail to log in with the following steps, please refer to **Manual Setup** on the bottom of this page.

### Step 1: Install the latest Arup Issue Tracker
[Download Installer (2019.7.15.1)](https://github.com/ArupAus/issue-tracker/releases/download/2019.7.15.1/Case_Issue_Tracker_2019.07.15.01.msi)

##### Please also make sure you have upgraded to Windows 10 and your Internet Explorer is up to date.
<img src="https://raw.githubusercontent.com/ArupAus/issue-tracker/master/Documentation/images/JiraCloudLogin_6.png" width="300">

### Step 2: Click “Add Jira Cloud Account” button in the Settings__
##### __DO NOT__ input your email and password directly. You must click the "Add Jira Cloud Account" button.
<img src="https://raw.githubusercontent.com/ArupAus/issue-tracker/master/Documentation/images/JiraCloudLogin_1.jpg" width="400">

### Step 3: Select or input Jira Cloud URL.
##### Please make sure which Jira Cloud instance you are using and select/input its URL.
<img src="https://raw.githubusercontent.com/ArupAus/issue-tracker/master/Documentation/images/JiraCloudLogin_2.jpg" width="400">

### Step 4: Log in with an existing Jira Cloud account.
<img src="https://raw.githubusercontent.com/ArupAus/issue-tracker/master/Documentation/images/JiraCloudLogin_3.jpg" width="400">

### Step 5: Login is successful when you see this message. Your Jira projects and issues will be loaded after clicking “OK”.
<img src="https://raw.githubusercontent.com/ArupAus/issue-tracker/master/Documentation/images/JiraCloudLogin_4.jpg" width="400">

## Mannual Setup
It is NOT recommended to input Jira Cloud account credentials manually. Please run through manual setup only when you fail to do the above steps.
- Step 1: Open browser and log into https://id.atlassian.com/manage/api-tokens
- Step 2: Click **Create API Token** button and create one with a concise name
- Step 3: Copy the generated API token to clipboard
- Step 4: Open Arup Issue Tracker (Desktop or plugins) and open Settings window
- Step 5: Input the followings: 
  - Active: select to enable
  - Jira Address: your Jira Cloud address https://xxxxxxxxxx.atlassian.net
  - Username/Email: your email address for Jira Cloud login
  - Password/Token: paste the generated API token here
- Step 6: Click Save button and login should proceed
