using System;

namespace Ipc
{
    public interface IBulkProgressNotification
    {
        event Action<double> BulkProgress;
    }
}