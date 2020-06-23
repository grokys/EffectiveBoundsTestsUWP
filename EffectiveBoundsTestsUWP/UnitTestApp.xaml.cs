using System;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace EffectiveBoundsTestsUWP
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private bool _isSplashScreenDismissed;
        private bool _isRootCreated = false;
        private List<Action> _actionsToRunAfterSplashScreenDismissedAndRootIsCreated = new List<Action>();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        public Frame RootFrame
        {
            get;
            set;
        }

        public static void RunAfterSplashScreenDismissed(Action action)
        {
            var app = Application.Current as App;
            lock (app._actionsToRunAfterSplashScreenDismissedAndRootIsCreated)
            {
                if (app._isSplashScreenDismissed && app._isRootCreated)
                {
                    action();
                }
                else
                {
                    app._actionsToRunAfterSplashScreenDismissedAndRootIsCreated.Add(action);
                }
            }
        }

        private void SplashScreen_Dismissed(SplashScreen sender, object args)
        {
            _isSplashScreenDismissed = true;
            if (_isRootCreated)
            {
                SplashScreenDismissedAndRootCreated();
            }
        }

        private void SplashScreenDismissedAndRootCreated()
        {
            lock (_actionsToRunAfterSplashScreenDismissedAndRootIsCreated)
            {
                foreach (var action in _actionsToRunAfterSplashScreenDismissedAndRootIsCreated)
                {
                    action();
                }
                _actionsToRunAfterSplashScreenDismissedAndRootIsCreated.Clear();
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            _isRootCreated = false;

            GC.Collect();

            e.SplashScreen.Dismissed += SplashScreen_Dismissed;

            Action createRoot = () =>
            {
                var rootFrame = Window.Current.Content as Frame;

                // Do not repeat app initialization when the Window already has content,
                // just ensure that the window is active
                if (rootFrame == null)
                {
                    // Create a Frame to act as the navigation context and navigate to the first page
                    rootFrame = new Frame();

                    rootFrame.NavigationFailed += OnNavigationFailed;

                    if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                    {
                        //TODO: Load state from previously suspended application
                    }

                    Window.Current.Content = rootFrame;
                }
                _isRootCreated = true;
                if (_isSplashScreenDismissed)
                {
                    SplashScreenDismissedAndRootCreated();
                }
            };

            // To exercise a couple different ways of setting up the tree, when run in APPX test mode then delay-attach the root.
            if (e.Arguments.Length == 0)
            {
                createRoot();
            }
            else
            {
                var uiDispatcher = Window.Current.Dispatcher;
                System.Threading.Tasks.Task.Delay(2000).ContinueWith(
                    (t) => {
                        var ignored = uiDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            createRoot();
                        });
                    });
            }

            Window.Current.Activated += OnWindowActivated;

            // Ensure the current window is active
            Window.Current.Activate();

            // If there are multiple arguments we assume we're being launched as a TAEF AppX test, so start up the TAEF dispatcher.
            if (e.Arguments.Length > 0)
            {
                // By default Verify throws exception on errors and exceptions cause TAEF AppX tests to fail in non-graceful ways
                // (we get the test failure and then TE keeps trying to talk to the crashing process so we get "TE session timed out" errors too).
                // Just disable exceptions in this scenario.
                //Verify.DisableVerifyFailureExceptions = true;
                Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.Run(e.Arguments);
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        private void OnWindowActivated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
        }

        public static void AppendResourceDictionaryToMergedDictionaries(ResourceDictionary dictionary)
        {
            // Check for null and dictionary not present
            if (!(dictionary is null) &&
                !Application.Current.Resources.MergedDictionaries.Contains(dictionary))
            {
                Application.Current.Resources.MergedDictionaries.Add(dictionary);
            }
        }

        public static void RemoveResourceDictionaryFromMergedDictionaries(ResourceDictionary dictionary)
        {
            // Check for null and dictionary is in list
            if (!(dictionary is null) &&
                Application.Current.Resources.MergedDictionaries.Contains(dictionary))
            {
                Application.Current.Resources.MergedDictionaries.Remove(dictionary);
            }
        }
    }
}
