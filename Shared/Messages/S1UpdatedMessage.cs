using Shared.Enums;
using Shared.Models;

namespace Shared.Messages
{
    public class S1UpdatedMessage
    {
        public Book BookForUpdate { get; set; } = null!;
        public ActionType ActionType { get; set; }
    }
}
