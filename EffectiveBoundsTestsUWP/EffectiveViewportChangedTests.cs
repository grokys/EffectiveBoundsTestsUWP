
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace EffectiveBoundsTestsUWP
{
    [TestClass]
    public class EffectiveViewportChangedTests
    {
        [TestMethod]
        public async Task EffectiveViewportChanged_Not_Raised_When_Control_Added_To_Tree()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var frame = GetFrame();
                var target = new Canvas();
                var raised = 0;

                target.EffectiveViewportChanged += (s, e) =>
                {
                    ++raised;
                };

                frame.Content = target;

                Assert.AreEqual(0, raised);
            });
        }

        [TestMethod]
        public async Task EffectiveViewportChanged_Raised_Before_LayoutUpdated()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var tcs = new TaskCompletionSource<object>();
                var frame = GetFrame();
                var target = new Canvas();
                var raised = 0;

                target.LayoutUpdated += (s, e) =>
                {
                    tcs.SetResult(null);
                };

                target.EffectiveViewportChanged += (s, e) =>
                {
                    ++raised;
                };

                frame.Content = target;

                await tcs.Task;
                Assert.AreEqual(1, raised);
            });
        }


        [TestMethod]
        public async Task Parent_Affects_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var tcs = new TaskCompletionSource<object>();
                var frame = GetFrame();
                var target = new Canvas
                {
                    Width = 100,
                    Height = 100,
                };

                var parent = new Border
                {
                    Width = 200,
                    Height = 200,
                    Child = target,
                };

                var raised = 0;

                frame.Content = parent;

                target.LayoutUpdated += (s, e) =>
                {
                    tcs.SetResult(null);
                };

                target.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.AreEqual(new Rect(-550, -400, 1200, 900), e.EffectiveViewport);
                    ++raised;
                };

                await tcs.Task;
            });
        }

        [TestMethod]
        public async Task Invalidating_In_Handler_Causes_Layout_To_Be_Rerun_Before_LayoutUpdated()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var frame = GetFrame();
                var target = new TestCanvas();
                var tcs = new TaskCompletionSource<object>();
                var raised = 0;

                target.LayoutUpdated += (s, e) =>
                {
                    Assert.AreEqual(2, target.MeasureCount);
                    Assert.AreEqual(2, target.ArrangeCount);
                    tcs.SetResult(null);
                };

                target.EffectiveViewportChanged += (s, e) =>
                {
                    target.InvalidateMeasure();
                    ++raised;
                };

                frame.Content = target;

                await tcs.Task;
                Assert.AreEqual(1, raised);
            });
        }

        [TestMethod]
        public async Task Viewport_Extends_Beyond_Centered_Control()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var frame = GetFrame();
                var target = new Canvas
                {
                    Width = 52,
                    Height = 52,
                };
                var tcs = new TaskCompletionSource<object>();
                var raised = 0;

                target.LayoutUpdated += (s, e) =>
                {
                    tcs.SetResult(null);
                };

                target.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.AreEqual(new Rect(-574, -424, 1200, 900), e.EffectiveViewport);
                    ++raised;
                };

                frame.Content = target;

                await tcs.Task;
                Assert.AreEqual(1, raised);
            });
        }

        [TestMethod]
        public async Task Viewport_Extends_Beyond_Nested_Centered_Control()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var frame = GetFrame();
                var target = new Canvas
                {
                    Width = 52,
                    Height = 52,
                };

                var parent = new Border
                {
                    Width = 100,
                    Height = 100,
                    Child = target,
                };

                var tcs = new TaskCompletionSource<object>();
                var raised = 0;

                target.LayoutUpdated += (s, e) =>
                {
                    tcs.SetResult(null);
                };

                target.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.AreEqual(new Rect(-574, -424, 1200, 900), e.EffectiveViewport);
                    ++raised;
                };

                frame.Content = parent;

                await tcs.Task;
                Assert.AreEqual(1, raised);
            });
        }

        [TestMethod]
        public async Task ScrollViewer_Determines_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var frame = GetFrame();
                var target = new Canvas
                {
                    Width = 200,
                    Height = 200,
                };

                var scroller = new ScrollViewer
                {
                    Width = 100,
                    Height = 100,
                    Content = target,
                };

                var tcs = new TaskCompletionSource<object>();
                var raised = 0;

                target.LayoutUpdated += (s, e) =>
                {
                    tcs.TrySetResult(null);
                };

                target.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.AreEqual(new Rect(0, 0, 100, 100), e.EffectiveViewport);
                    ++raised;
                };

                frame.Content = scroller;

                await tcs.Task;
                Assert.AreEqual(1, raised);
            });
        }

        [TestMethod]
        public async Task Scrolled_ScrollViewer_Determines_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var frame = GetFrame();
                var target = new Canvas
                {
                    Width = 200,
                    Height = 200,
                };

                var scroller = new ScrollViewer
                {
                    Width = 100,
                    Height = 100,
                    Content = target,
                };

                var raised = 0;

                frame.Content = scroller;

                // Wait for everything to be laid out initially.
                await RunOnUIThread.WaitForTick();

                target.EffectiveViewportChanged += (s, e) =>
                {
                    if (e.EffectiveViewport == new Rect(0, 10, 100, 100))
                    {
                        ++raised;
                    }
                };

                // Scroll and wait a while for the UI to update.
                scroller.ChangeView(null, 10, null);

                for (var i = 0; i < 1000 && raised == 0; ++i)
                {
                    await RunOnUIThread.WaitForTick();
                }

                Assert.AreEqual(1, raised);
            });
        }

        [TestMethod]
        public async Task Moving_Parent_Updates_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var frame = GetFrame();
                var target = new Canvas
                {
                    Width = 100,
                    Height = 100,
                };

                var parent = new Border
                {
                    Width = 200,
                    Height = 200,
                    Child = target,
                };

                var raised = 0;

                frame.Content = parent;

                // Wait for everything to be laid out initially.
                await RunOnUIThread.WaitForTick();

                target.EffectiveViewportChanged += (s, e) =>
                {
                    if (e.EffectiveViewport == new Rect(-554, -400, 1200, 900))
                    {
                        ++raised;
                    }
                };

                // Change the parent margin to move it.
                parent.Margin = new Thickness(8, 0, 0, 0);

                for (var i = 0; i < 100 && raised == 0; ++i)
                {
                    await RunOnUIThread.WaitForTick();
                }

                Assert.AreEqual(1, raised);
            });
        }

        [TestMethod]
        public async Task Translate_Transforming_Affects_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var frame = GetFrame();
                var target = new Canvas
                {
                    Width = 100,
                    Height = 100,
                };

                var parent = new Border
                {
                    Width = 200,
                    Height = 200,
                    Child = target,
                };

                var raised = 0;

                frame.Content = parent;

                // Wait for everything to be laid out initially.
                await RunOnUIThread.WaitForTick();

                target.EffectiveViewportChanged += (s, e) =>
                {
                    if (e.EffectiveViewport == new Rect(-558, -400, 1200, 900))
                    {
                        ++raised;
                    }
                };

                // Change the parent render transform to move it. A layout is then needed before
                // EffectiveViewportChanged is raised.
                target.RenderTransform = new TranslateTransform { X = 8 };
                target.InvalidateMeasure();

                for (var i = 0; i < 100 && raised == 0; ++i)
                {
                    await RunOnUIThread.WaitForTick();
                }

                Assert.AreEqual(1, raised);
            });
        }

        [TestMethod]
        public async Task Translate_Transform_On_Parent_Affects_EffectiveViewport()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var frame = GetFrame();
                var target = new Canvas
                {
                    Width = 100,
                    Height = 100,
                };

                var parent = new Border
                {
                    Width = 200,
                    Height = 200,
                    Child = target,
                };

                var raised = 0;

                frame.Content = parent;

                // Wait for everything to be laid out initially.
                await RunOnUIThread.WaitForTick();

                target.EffectiveViewportChanged += (s, e) =>
                {
                    if (e.EffectiveViewport == new Rect(-558, -400, 1200, 900))
                    {
                        ++raised;
                    }
                };

                // Change the parent render transform to move it. A layout is then needed before
                // EffectiveViewportChanged is raised.
                parent.RenderTransform = new TranslateTransform { X = 8 };
                parent.InvalidateMeasure();

                for (var i = 0; i < 100 && raised == 0; ++i)
                {
                    await RunOnUIThread.WaitForTick();
                }

                Assert.AreEqual(1, raised);
            });
        }

        private Frame GetFrame()
        {
            var result = Window.Current.Content as Frame;
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
