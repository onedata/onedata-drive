# Onedata Drive

**The application is currently under development. Available installation packages are alpha versions. It is not intended for use with production data!**

Onedata Drive is a graphical interface application allowing Windows users to work with data stored in Onedata system. 

## Requirements
- Supported Windows version: **Windows 10 (version 1803 +)** and **Windows 11**
- **.NET Desktop Runtime 8** must be installed
- Before the installation of the new version, **the old version has to be uninstalled**  (The installer will be improved later to make this step unnecessary)
- You should not need to run as admin, as long you have access to the Root Folder

## Running the app
- in order to access files Onedata Drive must be running
- every time you restart computer/app and connect again new session is created
- connecting to big spaces (with many folders) might take a long time (up to several minutes)
- **Oneprovider Token** must have REST/CDMI access
- If you want refresh folder (show changes perfomed on cloud): `Right Click` (On empty space) -> `Show more options`-> `Onedata Drive` -> `Refresh Current Folder`

### Filling the connect form
All options can be set in the graphicall user interface. You can fill the connect form manually or you can load existing configuration file (`Advanced` -> `Load configuration from file`). 

**Configuration JSON file example:**
```
{
    "onezone" : "datahub.egi.eu",
    "provider_token" : "TOKEN",
    "root_path" : "C:\\Users\\user\\Onedata\\"
}
```

### Configuration
 The configuration is stored in the file at `C:\Users\<username>\AppData\Local\OnedataDrive\UserSettings.config`. 

Selected configuration options:

- **Root Folder**
    - path to the local folder where directories and files are synced.
    - the original contents of the directory will be deleted when connecting.
- **Oneprovider token**
    - must have REST/CDMI access,
    - must have access to Oneproviders (Onezone access is not required),
    - limiting token access to specific paths (during token creation) might not work as expected with spaces of which you are not owner.

### Mount explanation
When successfully connected, all files and directories are virtually available in your local file system. However, they might be in different states depending on whether they have locally stored copy (*hydrated*) or not:
- **`Available when online`** - file or directory acts as a *placeholder* and doesn't store any data locally yet. Opening it will automatically *hydrate* the item to the local disk (switch to the `Availabe on this device` state).
- **`Available on this device`** - file or directory is present on the local disk (already *hydrated*), but the operating system may *dehydrate* it when needed (switch back to the `Available when online` state).
- **`Always available on this device`** - file or directory is present on the local disk (already *hydrated*), and the operating system will **not** *dehydrate* it. To make sure that a specific item stays *hydrated* you can *pin* / *unpin* it using context menu for that item and select `Always keep on this device`.

To remove the local copy of a file or directory (*dehydrate*) you can use context menu for that item and select `Free up space`.

<img src="docs/imgs/file-states.png" alt="file-states" style="max-height: 250">

### Allowed characters
Windows has more restrictive rules for file names than Unix-like systems. Windows is case-insensitive for naming files and directories and does not allow several characters and keywords. When mounting, Onedata Drive will show the affected items with a modified name to comply with the restrictions. Although the affected items on the mount are shown with modified names, the original names will be preserved on the Onedata system.

Examples of affected files:
| Onedata system | Mount in Windows    |
| ---            | ---                 |
| AUX.abc        | _AUX.abc@3646437313 |
| file*12        | file_12@3363832623  |
| FILE           | FILE                |
| file           | file@3346262633     |

### Logging
Files with logs can be found at `C:\Users\<username>\AppData\Local\OnedataDrive\logs\`. When the application is disconnected, the directory can be opened by clicking on the `Open folder with logs` button in the `Advanced` section.

## Troubleshooting
- If the app crashes and you can not reconnect to the cloud restarting computer should fix the issue.
- If the app is not running, but sync root (OneData) is still present you may try using `Advanced` -> `Remove SyncRoot`
- Check the logs for any errors or warnings.

## Limitations
- **Does not support moving files between spaces.**
- Does not work in Windows Sandbox. In virtual machine (e.g. in Hyper-V) it works fine.
- The application is not signed, so Windows Defender SmartScreen might block it. You can run it anyway by clicking `More info` and `Run anyway`.

## Acknowledgment
<p align="left">
  <img src="https://webcentrum.muni.cz/media/3831863/seda_eosc.png" alt="EOSC CZ Logo" style="max-height: 90">
</p>

---
This project output was developed with financial contributions from the [EOSC CZ](https://www.eosc.cz/projekty/narodni-podpora-pro-eosc) initiative throught the project **National Repository Platform for Research Data** (CZ.02.01.01/00/23_014/0008787) funded by Programme Johannes Amos Comenius (P JAC) of the Ministry of Education, Youth and Sports of the Czech Republic (MEYS).

---

<p align="left">
  <img src="https://webcentrum.muni.cz/media/3832168/seda_eu-msmt_eng.png" alt="EU and MŠMT Logos" style="max-height: 90">
</p>
