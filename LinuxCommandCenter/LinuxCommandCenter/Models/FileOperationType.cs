namespace LinuxCommandCenter.Models
{
    public enum FileOperationType
    {
        Copy,
        Move,
        Delete,
        Rename,
        CreateDirectory,
        CreateFile,
        ChangePermissions,
        ChangeOwner,
        Compress,
        Extract
    }
}