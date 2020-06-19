
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
        public async Task Not_Raised_When_Added_To_Tree()
        {
            await RunOnUIThread.Execute(() =>
            {
                var frame = CreateFrame();
                var button = new Button();
                var raised = 0;

                button.EffectiveViewportChanged += (s, e) =>
                {
                    ++raised;
                };

                frame.Content = button;

                Assert.AreEqual(0, raised);
            });
        }

        [TestMethod]
        public async Task Raised_Before_LayoutUpdated()
        {
            await RunOnUIThread.ExecuteAsync(async () =>
            {
                var frame = CreateFrame();
                var button = new Button { HorizontalAlignment = HorizontalAlignment.Center };
                var tcs = new TaskCompletionSource<object>();
                var raised = 0;

                button.LayoutUpdated += (s, e) =>
                {
                    tcs.SetResult(null);
                };

                button.EffectiveViewportChanged += (s, e) =>
                {
                    Assert.AreEqual(
                        new Rect(
                            -button.ActualOffset.X,
                            -button.ActualOffset.Y,
                            frame.ActualSize.X,
                            frame.ActualSize.Y),
                        e.EffectiveViewport);
                    ++raised;
                };

                frame.Content = button;

                await tcs.Task;
                Assert.AreEqual(1, raised);
            });
        }

        [TestMethod]
        public async Task Invalidating_In_Handler_Causes_Layout_To_Be_Rerun_Before_LayoutUpdated()
        {
            await RunOnUIThread.ExecuteAsync(async () =>
            {
                var frame = CreateFrame();
                var button = new TestButton { HorizontalAlignment = HorizontalAlignment.Center };
                var tcs = new TaskCompletionSource<object>();
                var raised = 0;

                button.LayoutUpdated += (s, e) =>
                {
                    Assert.AreEqual(2, button.MeasureCount);
                    Assert.AreEqual(2, button.ArrangeCount);
                    tcs.SetResult(null);
                };

                button.EffectiveViewportChanged += (s, e) =>
                {
                    button.InvalidateMeasure();
                    ++raised;
                };

                frame.Content = button;

                await tcs.Task;
                Assert.AreEqual(1, raised);
            });
        }

        private Frame CreateFrame()
        {
            var frame = new Frame();
            Window.Current.Content = frame;
            return frame;
        }

        private class TestButton : Button
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
