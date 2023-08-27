using Shared.Models;

namespace Shared.Common
{
    public class ErrorArgs
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string ErrorMessage { get; set; }
        public Book? OldValue { get; set; }
        public Book NewValue { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    }
}
