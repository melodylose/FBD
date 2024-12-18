using System;

namespace FBDApp.Services
{
    public class FileServiceException : Exception
    {
        public FileServiceException(string message) : base(message)
        {
        }

        public FileServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
