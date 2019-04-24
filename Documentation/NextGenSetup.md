# Jira Cloud Next-gen Project Setup Guide
This doc runs through a minimum setup for Next-gen project to work with Arup Issue Tracker. To increase userbase by simplifying Jira configuration, Atlassian has launched the Next-gen project last year on Jira Cloud (as opposed to Classic project). As it makes our lives easier (for both end-users and Jira project admins), we should be aiming to use it wherever it suits. Now you can DIY an Arup Issue Tracker compliant project on Jira Cloud w/o logging an IT ticket. A couple of quick facts about "Next gen":

* It's more like Trello. More user friendly and easier to use.
* It's also easier to create and configure projects. Project settings are isolated among projects. Jira Project Lead can tweak their projects without System Admin's help.
* It's a trade-off between usability and functionality. There is limitation though Atlassian is adding more and more functions from "Classic" to "Next-gen".

In a nutshell, Next-gen is recommended for most use cases. However, this is NOT a lesson 101 tutorial for Next-gen. Please look through the references below for detailed usage before creating a Next-gen project. If not, it should be fairly straightforward if you are an active Jira user. As a side note, Next-gen project is only available on Jira Cloud (https://ovearup.atlassian.net). Our internal Jira Server (http://jira.arup.com) doesn't have this functionality. The logins are different.


### Next-gen Project References
[Video Podcast](https://www.youtube.com/watch?v=PQa3NFB_LRg&list=PLaD4FvsFdarR69HESUlY4IC7ae5k5znYp)

[User Guide](https://confluence.atlassian.com/jirasoftwarecloud/working-with-next-gen-software-projects-945104895.html)

[Classic vs. Next-gen Comparison](https://community.atlassian.com/t5/Next-gen-articles/Everything-you-want-to-know-about-next-gen-projects-in-Jira/ba-p/894773)



### Access to Jira Cloud (Atlassian Cloud)
Note that the following actions are separate things:
* #### Create a Jira Cloud account
If you haven't got a Jira Cloud account or you want to create accounts for external users, please go to
[Jira Self Service Portal (Arup staffs only)](http://to.arup.com/jssp).
* #### Create a Next-gen projet
You can do this once you have a Jira Cloud account. Look at the step 2 and 3 below.
* #### Add existing Jira Cloud users to a Next-gen project
You can do this in __Project Settings > People__.


### Set up a Next-gen Project for Arup Issue Tracker
#### Step 1: Install the latest Arup Issue Tracker
[Download Installer (2019.4.17.1)](https://github.com/ArupAus/issue-tracker/releases/download/2019.4.17.1/Case_Issue_Tracker_2019.04.17.01.msi)

##### Please also make sure you have upgraded to Windows 10 and your Internet Explorer is up to date.
<img src="https://raw.githubusercontent.com/ArupAus/issue-tracker/master/Documentation/images/JiraCloudLogin_6.png" width="300">

#### Step 2: Create a Next-gen Project on Jira Cloud
Go to [Projects](https://ovearup.atlassian.net/secure/BrowseProjects.jspa) > __Create project__ on the top right > __Try a next-gen project__.
<img src="https://raw.githubusercontent.com/ArupAus/issue-tracker/master/Documentation/images/nextgen1.png" width="400">

#### Step 3: Choose Template, Project Name, Project Key, and Access Level
* Select a Next-gen project template. It's recommended to use __Kanban__ for common issue tracking or task management.
* Input a project name. It's recommended to have job number as prefix, e.g., __123456-78 My Own Project__.
* Expand __Advanced__ and change auto-generated project key to something makes sense, such as project acronym.
* Change __Access__ to __Private__. This ensures confidentiality as we've got lots of external users. If your project access level is not set to __Private__, our Jira system automation program running on a daily basis will make the change for you.

<img src="https://raw.githubusercontent.com/ArupAus/issue-tracker/master/Documentation/images/nextgen2.png" width="800">

#### Step 4: Create a GUID Field for Every Issue Type
This is the most important step. You can create as many issue types as you want in __Project Settings__. Just remember to add the __GUID__ field to every issue type and __Save Changes__ afterwards. If you haven't created a __Text__ field called __GUID__ in a project, please create one from the right panel and then drag/drop it into either __Primary Fields__ or __Secondary Fields__ for each issue type.

<img src="https://raw.githubusercontent.com/ArupAus/issue-tracker/master/Documentation/images/nextgen4.png" width="800">

#### Step 5: Change Project Category
For better usage tracking, it's also recommended to change __Project Category__ to __Arup Issue Tracker__. You can change this via __Project Settings__ > __Details__ > __Category__
<img src="https://raw.githubusercontent.com/ArupAus/issue-tracker/master/Documentation/images/nextgen3.png" width="400">
