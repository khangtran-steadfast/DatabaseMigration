using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseMigration.Infrastructure.Exceptions
{
    /// <summary>
    /// Custom Exception class for all data migration exceptions. 
    /// 
    /// This exception implementation attempts to map the provided error to one of the predefined errors
    /// stored in the internal string table. If an entry is found, the internal error message of this exception is set
    /// to the looked up error message, and the original message assumes the role of error code.
    /// </summary>
    public class MigrationException : ApplicationException
    {
        /// <summary>
        /// Basic implementation.
        /// </summary>
        /// <param name="error"></param>
        public MigrationException(string errorCode, params object[] placeholders)
            : base(GetMessage(errorCode, placeholders))
        {
            this.ErrorCode = errorCode;
        }

        /// <summary>
        /// Does the mapping of the provided error code to one of the stored error messages.
        /// </summary>
        /// <param name="error"></param>
        private static string GetMessage(string errorCode, object[] placeholders)
        {
            string errorMessage = Resources.ResourceManager.GetString(errorCode);
            if (errorMessage == null)
            {
                return errorCode;
            }
            else if (placeholders == null || placeholders.Length == 0)
            {
                return errorMessage;
            }
            else
            {
                return string.Format(errorMessage, placeholders);
            }
        }

        /// <summary>
        /// Initialises an exception object with the provided inner exception object.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public MigrationException(Exception ex, string errorCode, params object[] placeholders)
            : base(GetMessage(errorCode, placeholders), ex)
        {
            this.ErrorCode = errorCode;
        }

        public string ErrorCode { get; private set; }
    }
}
