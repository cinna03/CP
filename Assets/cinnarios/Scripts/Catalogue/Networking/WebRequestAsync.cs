using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Cinnarios.Catalogue.Networking
{
    /// <summary>
    /// Lets a UnityWebRequest be awaited with async/await, so network code reads top to
    /// bottom instead of using coroutines. Code after the await continues on Unity's main thread.
    /// Cancelling (for example when a scene closes) aborts only requests that are still running.
    /// </summary>
    public static class WebRequestAsync
    {
        public static async Task SendAsync(UnityWebRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Continuations run asynchronously so the caller can never dispose the request
            // while the cancel callback below is still using it.
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            operation.completed += _ => completion.TrySetResult(true);
            if (operation.isDone)
            {
                completion.TrySetResult(true);
            }

            // The registration is removed as soon as the request finishes, so a later cancel
            // never touches a request that has already been cleaned up.
            using (cancellationToken.Register(() =>
                   {
                       if (!completion.Task.IsCompleted)
                       {
                           request.Abort();
                           completion.TrySetCanceled(cancellationToken);
                       }
                   }))
            {
                await completion.Task;
            }
        }
    }
}
