
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace EffectiveBoundsTestsUWP
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task EffectiveViewportChanged_Not_Raised_When_Control_Added_To_Tree()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var frame = GetFrame();
                var canvas = new Canvas();
                var raised = 0;

                canvas.EffectiveViewportChanged += (s, e) =>
                {
                    ++raised;
                };

                frame.Content = canvas;

                Assert.AreEqual(0, raised);
            });
        }

        [TestMethod]
        public async Task EffectiveViewportChanged_Raised_Before_LayoutUpdated()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var frame = GetFrame();
                var canvas = new Canvas();
                var tcs = new TaskCompletionSource<object>();
                var raised = 0;

                canvas.LayoutUpdated += (s, e) =>
                {
                    tcs.SetResult(null);
                };

                canvas.EffectiveViewportChanged += (s, e) =>
                {
                    ++raised;
                };

                frame.Content = canvas;

                await tcs.Task;
                Assert.AreEqual(1, raised);
            });
        }

        [TestMethod]
        public async Task Invalidating_In_Handler_Causes_Layout_To_Be_Rerun_Before_LayoutUpdated()
        {
            await RunOnUIThread.Execute(async () =>
            {
                var frame = GetFrame();
                var canvas = new TestCanvas();
                var tcs = new TaskCompletionSource<object>();
                var raised = 0;

                canvas.LayoutUpdated += (s, e) =>
                {
                    Assert.AreEqual(2, canvas.MeasureCount);
                    Assert.AreEqual(2, canvas.ArrangeCount);
                    tcs.SetResult(null);
                };

                canvas.EffectiveViewportChanged += (s, e) =>
                {
                    canvas.InvalidateMeasure();
                    ++raised;
                };

                frame.Content = canvas;

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
                var canvas = new Canvas
                {
                    Width = 52,
                    Height = 52,
                };
                var tcs = new TaskCompletionSource<object>();
                var raised = 0;

                canvas.LayoutUpdated += (s, e) =>
                {
                    tcs.SetResult(null);
                };

                canvas.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.AreEqual(new Rect(-574, -424, 1200, 900), e.EffectiveViewport);
                    ++raised;
                };

                frame.Content = canvas;

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
                var canvas = new Canvas
                {
                    Width = 52,
                    Height = 52,
                };

                var outer = new Border
                {
                    Width = 100,
                    Height = 100,
                    Child = canvas,
                };

                var tcs = new TaskCompletionSource<object>();
                var raised = 0;

                canvas.LayoutUpdated += (s, e) =>
                {
                    tcs.SetResult(null);
                };

                canvas.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.AreEqual(new Rect(-574, -424, 1200, 900), e.EffectiveViewport);
                    ++raised;
                };

                frame.Content = outer;

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
                var canvas = new Canvas
                {
                    Width = 200,
                    Height = 200,
                };

                var outer = new ScrollViewer
                {
                    Width = 100,
                    Height = 100,
                    Content = canvas,
                };

                var tcs = new TaskCompletionSource<object>();
                var raised = 0;

                canvas.LayoutUpdated += (s, e) =>
                {
                    tcs.TrySetResult(null);
                };

                canvas.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.AreEqual(new Rect(0, 0, 100, 100), e.EffectiveViewport);
                    ++raised;
                };

                frame.Content = outer;

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
                var canvas = new Canvas
                {
                    Width = 200,
                    Height = 200,
                };

                var outer = new ScrollViewer
                {
                    Width = 100,
                    Height = 100,
                    Content = canvas,
                };

                var tcs = new TaskCompletionSource<object>();
                var raised = 0;

                canvas.LayoutUpdated += (s, e) =>
                {
                    if (raised > 0)
                    {
                        tcs.TrySetResult(null);
                    }
                };

                canvas.EffectiveViewportChanged += (s, e) =>
                {
                    if (outer.VerticalOffset == 10)
                    {
                        Assert.AreEqual(new Rect(0, -10, 100, 100), e.EffectiveViewport);
                        ++raised;
                    }
                };

                frame.Content = outer;
                outer.ChangeView(null, 10, null);

                await tcs.Task;
                Assert.AreEqual(1, raised);
            });
        }

        private Frame GetFrame() => Window.Current.Content as Frame;

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
