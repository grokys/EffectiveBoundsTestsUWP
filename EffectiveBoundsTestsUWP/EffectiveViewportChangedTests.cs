
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace EffectiveBoundsTestsUWP
{
    // These tests are ported from https://github.com/grokys/EffectiveBoundsTestsUWP
    // The weird boilerplate comes from the UWP tests, I tried to port them with the minimum changes
    // possible.
    [TestClass]
    public class EffectiveViewportChangedTests
    {
        [TestMethod]
        public async Task EffectiveViewportChanged_Not_Raised_When_Control_Added_To_Tree()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas();
                var raised = 0;

                target.EffectiveViewportChanged += (s, e) =>
                {
                    ++raised;
                };

                root.Content = target;

                Assert.AreEqual(0, raised);
            });
        }

        [TestMethod]
        public async Task EffectiveViewportChanged_Raised_Before_LayoutUpdated()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas();
                var raised = 0;

                target.EffectiveViewportChanged += (s, e) =>
                {
                    ++raised;
                };

                root.Content = target;

                await ExecuteInitialLayoutPass(root);

                Assert.AreEqual(1, raised);
            });
        }

        [TestMethod]
        public async Task Parent_Affects_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas { Width = 100, Height = 100 };
                var parent = new Border { Width = 200, Height = 200, Child = target };
                var raised = 0;

                root.Content = parent;

                target.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.AreEqual(new Rect(-550, -400, 1200, 900), e.EffectiveViewport);
                    ++raised;
                };

                await ExecuteInitialLayoutPass(root);
            });
        }

        [TestMethod]
        public async Task Invalidating_In_Handler_Causes_Layout_To_Be_Rerun_Before_LayoutUpdated_Raised()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new TestCanvas();
                var raised = 0;
                var layoutUpdatedRaised = 0;

                root.LayoutUpdated += (s, e) =>
                {
                    Assert.AreEqual(2, target.MeasureCount);
                    Assert.AreEqual(2, target.ArrangeCount);
                    ++layoutUpdatedRaised;
                };

                target.EffectiveViewportChanged += (s, e) =>
                {
                    target.InvalidateMeasure();
                    ++raised;
                };

                root.Content = target;

                await ExecuteInitialLayoutPass(root);
                
                Assert.AreEqual(1, raised);
                Assert.AreEqual(1, layoutUpdatedRaised);
            });
        }

        [TestMethod]
        public async Task Viewport_Extends_Beyond_Centered_Control()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas { Width = 52, Height = 52, };
                var raised = 0;

                target.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.AreEqual(new Rect(-574, -424, 1200, 900), e.EffectiveViewport);
                    ++raised;
                };

                root.Content = target;

                await ExecuteInitialLayoutPass(root);
                Assert.AreEqual(1, raised);
            });
        }

        [TestMethod]
        public async Task Viewport_Extends_Beyond_Nested_Centered_Control()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas { Width = 52, Height = 52 };
                var parent = new Border { Width = 100, Height = 100, Child = target };
                var raised = 0;

                target.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.AreEqual(new Rect(-574, -424, 1200, 900), e.EffectiveViewport);
                    ++raised;
                };

                root.Content = parent;

                await ExecuteInitialLayoutPass(root);
                Assert.AreEqual(1, raised);
            });
        }

        [TestMethod]
        public async Task ScrollViewer_Determines_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas { Width = 200, Height = 200 };
                var scroller = new ScrollViewer { Width = 100, Height = 100, Content = target };
                var raised = 0;

                target.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.AreEqual(new Rect(0, 0, 100, 100), e.EffectiveViewport);
                    ++raised;
                };

                root.Content = scroller;

                await ExecuteInitialLayoutPass(root);
                Assert.AreEqual(1, raised);
            });
        }

        [TestMethod]
        public async Task Scrolled_ScrollViewer_Determines_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas { Width = 200, Height = 200 };
                var scroller = new ScrollViewer { Width = 100, Height = 100, Content = target };
                var raised = 0;

                root.Content = scroller;

                await ExecuteInitialLayoutPass(root);
                scroller.ChangeView(null, 10, null);

                await ExecuteScrollerLayoutPass(root, scroller, target, (s, e) =>
                {
                    Assert.AreEqual(new Rect(0, 10, 100, 100), e.EffectiveViewport);
                    ++raised;
                });

                Assert.AreEqual(1, raised);
            });
        }

        [TestMethod]
        public async Task Moving_Parent_Updates_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas { Width = 100, Height = 100 };
                var parent = new Border { Width = 200, Height = 200, Child = target };
                var raised = 0;

                root.Content = parent;

                await ExecuteInitialLayoutPass(root);

                target.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.AreEqual(new Rect(-554, -400, 1200, 900), e.EffectiveViewport);
                    ++raised;
                };

                parent.Margin = new Thickness(8, 0, 0, 0);
                await ExecuteLayoutPass(root);

                Assert.AreEqual(1, raised);
            });
        }

        [TestMethod]
        public async Task Translate_Transform_Doesnt_Affect_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas { Width = 100, Height = 100 };
                var parent = new Border { Width = 200, Height = 200, Child = target };
                var raised = 0;

                root.Content = parent;

                await ExecuteInitialLayoutPass(root);
                target.EffectiveViewportChanged += (s, e) => ++raised;
                target.RenderTransform = new TranslateTransform { X = 8 };
                target.InvalidateMeasure();
                await ExecuteLayoutPass(root);

                Assert.AreEqual(0, raised);
            });
        }

        [TestMethod]
        public async Task Translate_Transform_On_Parent_Affects_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas { Width = 100, Height = 100 };
                var parent = new Border { Width = 200, Height = 200, Child = target };
                var raised = 0;

                root.Content = parent;

                await ExecuteInitialLayoutPass(root);

                target.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.AreEqual(new Rect(-558, -400, 1200, 900), e.EffectiveViewport);
                    ++raised;
                };

                // Change the parent render transform to move it. A layout is then needed before
                // EffectiveViewportChanged is raised.
                parent.RenderTransform = new TranslateTransform { X = 8 };
                parent.InvalidateMeasure();
                await ExecuteLayoutPass(root);

                Assert.AreEqual(1, raised);
            });
        }

        [TestMethod]
        public async Task Rotate_Transform_On_Parent_Affects_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var root = CreateRoot();
                var target = new Canvas { Width = 100, Height = 100 };
                var parent = new Border { Width = 200, Height = 200, Child = target };
                var raised = 0;

                root.Content = parent;

                await ExecuteInitialLayoutPass(root);

                target.EffectiveViewportChanged += (s, e) =>
                {
                    AssertArePixelEqual(new Rect(-651, -792, 1484, 1484), e.EffectiveViewport);
                    ++raised;
                };

                parent.RenderTransform = new RotateTransform { Angle = 45 };
                parent.InvalidateMeasure();
                await ExecuteLayoutPass(root);

                Assert.AreEqual(1, raised);
            });
        }

        private async Task ExecuteLayoutPass(Frame root)
        {
            var tcs = new TaskCompletionSource<object>();

            void LayoutUpdated(object sender, object e)
            {
                tcs.SetResult(null);
                root.LayoutUpdated -= LayoutUpdated;
            }

            root.LayoutUpdated += LayoutUpdated;
            await tcs.Task;
        }

        private Task ExecuteInitialLayoutPass(Frame root) => ExecuteLayoutPass(root);

        private async Task ExecuteScrollerLayoutPass(
            Frame root,
            ScrollViewer scroller,
            FrameworkElement target,
            Action<FrameworkElement, EffectiveViewportChangedEventArgs> handler)
        {
            var viewChangedRaised = false;
            var viewportChangedRaised = 0;

            void ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
            {
                scroller.ViewChanged -= ViewChanged;
                viewChangedRaised = !e.IsIntermediate;
            }

            void ViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs e)
            {
                if (viewChangedRaised)
                {
                    // We need to ignore the first EffectiveViewportChanged for some reason as it's
                    // still not up-to-date.
                    if (viewportChangedRaised++ > 0)
                    {
                        handler(sender, e);
                    }
                }
            }

            scroller.ViewChanged += ViewChanged;
            target.EffectiveViewportChanged += ViewportChanged;

            for (var i = 0; i < 50; ++i)
            {
                await ExecuteLayoutPass(root);

                if (viewportChangedRaised > 1)
                {
                    break;
                }
            }
        }

        private void AssertArePixelEqual(Rect expected, Rect actual)
        {
            var expectedRounded = new Rect((int)expected.X, (int)expected.Y, (int)expected.Width, (int)expected.Height);
            var actualRounded = new Rect((int)actual.X, (int)actual.Y, (int)actual.Width, (int)actual.Height);
            Assert.AreEqual(expectedRounded, actualRounded);
        }

        private Frame CreateRoot()
        {
            var result = new Frame();
            Window.Current.Content = result;
            result.UseLayoutRounding = false;
            return result;
        }

        private class TestCanvas : Canvas
        {
            public int MeasureCount { get; private set; }
            public int ArrangeCount { get; private set; }

            protected override Size MeasureOverride(Size availableSize)
            {
                ++MeasureCount;
                return base.MeasureOverride(availableSize);
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                ++ArrangeCount;
                return base.ArrangeOverride(finalSize);
            }
        }
    }
}
