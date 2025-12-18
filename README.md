# Onedata Drive

**The application is currently under development. Available installation packages are alpha versions. It is not intended for use with production data!**

Onedata Drive is a GUI application allowing Windows users to work with data stored in Onedata system. 

<img width="586" height="356" alt="Onedata Drive GUI" src="https://github.com/user-attachments/assets/a793e590-d901-4702-9a58-b9a8d3cd346a" />

To log in, you need to fill in the desired instance of the Onezone service and the token. The spaces available to the user are then mounted to the user's home directory.

<img width="647" height="423" alt="Mounted spaces from Onedata" src="https://github.com/user-attachments/assets/affdd129-77aa-4d66-9f73-6aacfc3d81f7" />

## Requirements
- Supported Windows version: **Windows 10 (version 1803 +)** and **Windows 11**
- **.NET Desktop Runtime 9** must be installed
- Before the installation of the new version, **versions older than 0.5.x should be uninstalled**
- You should not need to run as admin, as long you have access to the Root Folder
- Compatible only with **Oneprovider event-sse-v3**

## Running the app
- in order to access files Onedata Drive must be running
- every time you restart computer/app and connect again new session is created
- **Oneprovider Token** must have REST/CDMI access
- changes performed on cloud side are synced usually within 60s

### Filling the connect form
All options can be set in the graphicall user interface. You can fill the connect form manually or you can load existing configuration file (`Advanced` -> `Load configuration from file`). 

**Configuration JSON file example:**
```
{
    "onezone" : "datahub.egi.eu",
    "provider_token" : "TOKEN",
    "root_path" : "C:\\Users\\user\\Onedata Drive\\"
}
```

### Configuration

Selected configuration options:

- **Root Folder**
    - path to the local folder where directories and files are synced,
    - the original contents of the directory will be deleted when connecting.
- **Oneprovider token**
    - must have REST/CDMI access,
    - must have access to Oneproviders (Onezone access is not required),
    - limiting token access to specific paths (during token creation) might not work as expected.

## Logging
Files with logs can be found at `C:\Users\Andrej\AppData\Local\Packages\<packageName>\LocalState\Logs`. 

## In case of app failure
- If the app crashes and you can not reconnect to the cloud restarting computer should fix the issue.
- If the app is not connected, but sync root (Onedata folder) is still present you may try using `Advanced -> Remove SyncRoot`

## Notes
- Does not work in Windows Sandbox. In virtual machine (e.g. in Hyper-V) it works fine.
- The develop versions of the application are signed with certificate which is not trusted by default in Windows. 

## Acknowledgment
<p align="left">
  <img src="https://webcentrum.muni.cz/media/3831863/seda_eosc.png" alt="EOSC CZ Logo" height="90">
</p>

---
This project output was developed with financial contributions from the [EOSC CZ](https://www.eosc.cz/projekty/narodni-podpora-pro-eosc) initiative throught the project **National Repository Platform for Research Data** (CZ.02.01.01/00/23_014/0008787) funded by Programme Johannes Amos Comenius (P JAC) of the Ministry of Education, Youth and Sports of the Czech Republic (MEYS).

---

<p align="left">
  <img src="https://webcentrum.muni.cz/media/3832168/seda_eu-msmt_eng.png" alt="EU and MŠMT Logos" height="90">
</p>
