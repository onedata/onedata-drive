# Onedata Drive

Onedata Drive is a graphical interface application allowing Windows users to work with data stored in Onedata system. 

## Requirements
- Supported Windows version: **Windows 10 (version 1803)** and **Windows 11**
- **.NET Desktop Runtime 8** must be installed
- Might **not work** in **Windows Sandbox**
- Virtual machines (e.g. Hyper-V) should work fine
- You should not need to run as admin, as long you have access to the Root Folder

## Running the app
- in order to access files Onedata Drive must be running
- every time you restart computer/app and connect again new session is created
- connecting to big spaces (with many folders) might take a long time (up to several minutes)
- **Oneprovider Token** must have REST/CDMI access

### Filling the connect form
All options can be set in the graphicall user interface. You can fill the connect form manually or you can load existing configuration file (`Advanced` -> `Load configuration from file`). 

### Configuration
 The configuration is stored in the file at `C:\Users\username\AppData\Local\OnedataDrive\UserSettings.config`. 

Selected configuration options:

- **Root Folder**
    - path to the local folder where directories and files are synced.
    - the original contents of the directory will be deleted when connecting.
- **Oneprovider token**
    - must have REST/CDMI access,
    - must have access to Oneproviders (Onezone access is not required),
    - limiting token access to specific paths (during token creation) might not work as expected with spaces of which you are not owner.

## Logging
Files with logs can be found at `C:\Users\username\AppData\Local\OnedataDrive\logs\`. 

## Required packages (for develompent and compilation)
``
dotnet add package Vanara.PInvoke.CldApi
``

- install to use win32 api (cloudfilter)
