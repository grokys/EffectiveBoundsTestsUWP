using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public static Task Execute(Action action)
        {
            return Execute(CoreApplication.MainView, action);
        }

        public static Task ExecuteAsync(Func<Task> action)
        {
            return ExecuteAsync(CoreApplication.MainView, action);
        }

        public static async Task Execute(CoreApplicationView whichView, Action action)
        {
            var dispatcher = whichView.Dispatcher;

            if (dispatcher.HasThreadAccess)
            {
                action();
            }
            else
            {
                var tcs = new TaskCompletionSource<object>();

                void Run()
                {
                    try
                    {
                        action();
                        tcs.SetResult(null);
                    }
                    catch (Exception e)
                    {
                        tcs.SetException(e);
                    }
                }

                // We're not on the UI thread, queue the work. Make sure that the action is not run until
                // the splash screen is dismissed (i.e. that the window content is present).
                App.RunAfterSplashScreenDismissed(() =>
                {
                    // If the Splash screen dismissal happens on the UI thread, run the action right now.
                    if (dispatcher.HasThreadAccess)
                    {
                        Run();
                    }
                    else
                    {
                        // Otherwise queue the work to the UI thread and then set the completion event on that thread.
                        var ignore = dispatcher.RunAsync(CoreDispatcherPriority.Normal, Run);
                    }
                });

                await tcs.Task;
            }
        }

        public static async Task ExecuteAsync(CoreApplicationView whichView, Func<Task> action)
        {
            var dispatcher = whichView.Dispatcher;

            if (dispatcher.HasThreadAccess)
            {
                await action();
            }
            else
            {
                var tcs = new TaskCompletionSource<object>();

                async Task Run()
                {
                    try
                    {
                        await action();
                        tcs.SetResult(null);
                    }
                    catch (Exception e)
                    {
                        tcs.SetException(e);
                    }
                }

                // We're not on the UI thread, queue the work. Make sure that the action is not run until
                // the splash screen is dismissed (i.e. that the window content is present).
                App.RunAfterSplashScreenDismissed(() =>
                {
                    // If the Splash screen dismissal happens on the UI thread, run the action right now.
                    if (dispatcher.HasThreadAccess)
                    {
                        Run();
                    }
                    else
                    {
                        // Otherwise queue the work to the UI thread and then set the completion event on that thread.
                        var ignore = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Run());
                    }
                });

                await tcs.Task;
            }
        }

        public async Task WaitForTick()
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
