using Shared.Common;
using Shared.Enums;
using Shared.Models;

namespace Shared.Messages
{
    public class S2UpdatedResponse
    {
        public Book? UpdatedBook { get; set; } = null!;
        public ActionType ActionType { get; set; }
        public bool IsSuccess { get; set; }
        public ErrorArgs? ErrorArgs { get; set; }
    }
}
