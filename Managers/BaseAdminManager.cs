using System;

namespace TennisCatalog
{
    public abstract class BaseAdminManager
    {
        protected readonly DatabaseHelper db;
        protected readonly Action<string> showMessage;

        protected BaseAdminManager(DatabaseHelper db, Action<string> showMessage)
        {
            this.db = db ?? throw new ArgumentNullException(nameof(db));
            this.showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
        }

        protected string GetPrefix(string tour)
        {
            return tour == "ATP" ? "male" : "female";
        }
    }
}
