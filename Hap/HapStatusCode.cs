namespace HomeKit.Hap
{
    internal enum HapStatusCode : int
    {
        Success = 0,
        InsufficientPrivileges = -70401,
        UnableToPerformOperation = -70402,
        ResourceIsBusy = -70403,
        CannotWriteToReadOnly = -70404,
        CannotReadFromWriteOnly = -70405,
        NotificationNotSupported = -70406,
        OutOfResources = -70407,
        OperationTimedOut = -70408,
        ResourceDoesNotExist = -70409,
        InvalidValueInWriteRequest = -70410,
        InsufficientAuthorization = -70411,
    }
}
