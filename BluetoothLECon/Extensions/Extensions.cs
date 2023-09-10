namespace BluetoothLECon
{
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.Foundation;

    internal static class Extensions
    {
        public static void RunAsyncBackground<T>(this Task<T> task)
        {
            task.ContinueWith(t => Trace.WriteLine(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        public static void RunAsyncBackground(this Task task)
        {
            task.ContinueWith(t => Trace.WriteLine(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        public static void RunAsyncBackground<T>(this IAsyncOperation<T> task)
        {
            task.AsTask().RunAsyncBackground();
        }

        private static List<ContentDialog> DialogQueue = new();
        public static void Alert(this UIElement elem, string title, string content)
        {
            void consumeQueue()
            {
                DialogQueue.First().ShowAsync().RunAsyncBackground();
            }
            var dialog = new ContentDialog { Title = title, Content = content, CloseButtonText = "OK", XamlRoot = elem.XamlRoot };
            dialog.Closed += (sender, args) =>
            {
                DialogQueue.Remove(dialog);
                if (DialogQueue.Count > 0)
                {
                    consumeQueue();
                }
            };
            DialogQueue.Add(dialog);

            if (DialogQueue.Count == 1)
            {
                consumeQueue();
            }
        }

        public static void Alert(this Window window, string title, string content)
        {
            window.Content.Alert(title, content);
        }
    }
}
