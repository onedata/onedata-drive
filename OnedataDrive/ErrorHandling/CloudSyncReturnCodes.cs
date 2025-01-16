namespace OnedataDrive.ErrorHandling
{
    public enum CloudSyncReturnCodes : uint
    {
        SUCCESS = 0,
        ERROR = 1,
        ROOT_FOLDER_NOT_EMPTY,
        ROOT_FOLDER_NO_ACCESS_RIGHT,
        ONEZONE_FAIL,
        TOKEN_FAIL,
        INVALID_TOKEN_TYPE
    }
}
