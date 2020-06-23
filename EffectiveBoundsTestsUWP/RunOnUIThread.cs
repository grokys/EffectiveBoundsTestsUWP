using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;

namespace EffectiveBoundsTestsUWP
{
    public class RunOnUIThread
    {
        public static Task Execute(Func<Task> action)
        {
            return Execute(CoreApplication.MainView, action);
        }

        public static async Task Execute(CoreApplicationView whichView, Func<Task> action)
        {
            Exception exception = null;
            var dispatcher = whichView.Dispatcher;
            if (dispatcher.HasThreadAccess)
            {
                await action();
            }
            else
            {
                // We're not on the UI thread, queue the work. Make sure that the action is not run until
                // the splash screen is dismissed (i.e. that the window content is present).
                var workComplete = new AutoResetEvent(false);
                App.RunAfterSplashScreenDismissed(async () =>
                {
                    // If the Splash screen dismissal happens on the UI thread, run the action right now.
                    if (dispatcher.HasThreadAccess)
                    {
                        try
                        {
                            await action();
                        }
                        catch (Exception e)
                        {
                            exception = e;
                            throw;
                        }
                        finally // Unblock calling thread even if action() throws
                        {
                            workComplete.Set();
                        }
                    }
                    else
                    {
                        // Otherwise queue the work to the UI thread and then set the completion event on that thread.
                        var ignore = dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            async () =>
                            {
                                try
                                {
                                    await action();
                                }
                                catch (Exception e)
                                {
                                    exception = e;
                                    throw;
                                }
                                finally // Unblock calling thread even if action() throws
                                {
                                    workComplete.Set();
                                }
                            });
                    }
                });

                workComplete.WaitOne();
                if (exception != null)
                {
                    Assert.Fail("Exception thrown by action on the UI thread: " + exception.ToString());
                }
            }
        }

        public static async Task WaitForTick()
        {
            var renderingEventFired = new TaskCompletionSource<object>();

            EventHandler<object> renderingCallback = (sender, arg) =>
            {
                renderingEventFired.TrySetResult(null);
            };
            CompositionTarget.Rendering += renderingCallback;

            await renderingEventFired.Task;

            CompositionTarget.Rendering -= renderingCallback;
        }
    }
}
