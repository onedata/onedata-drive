# Release notes for project Onedata Drive

## 25.1.3
- Handle symlinks as regular files/directories
- **LIMITATIONS**:
	- Hydrated symlink placeholders are not refreshed when the symlink target is updated. The user must free the local data and download it again.
	- Changing symlink target type (DIR/REG) is not supported

## 25.1.2
- Change app display name

## 25.1.1
- Ignore symlinks
- Dehydrate local file when it was changed in the cloud

## 25.1.0
- Initial version
