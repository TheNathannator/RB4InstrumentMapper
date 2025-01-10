using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// A text writer which writes to a WPF textbox.
    /// </summary>
    /// <remarks>
    /// https://social.technet.microsoft.com/wiki/contents/articles/12347.wpfhowto-add-a-debugoutput-console-to-your-application.aspx
    /// </remarks>
    public class TextBoxWriter : TextWriter
    {
        private const int MaxLines = 100;

        private readonly object writeLock = new object();
        private readonly StringBuilder currentLine = new StringBuilder();
        private readonly Queue<string> visibleLines = new Queue<string>(MaxLines);

        private readonly TextBox textBox;
        private readonly Action updateText;
        private volatile bool updateQueued;

        public override Encoding Encoding => Encoding.Unicode;

        public TextBoxWriter(TextBox output)
        {
            textBox = output;
            updateText = UpdateText;
        }

        private void UpdateText()
        {
            lock (writeLock)
            {
                bool doScroll = Math.Abs(textBox.ExtentHeight - (textBox.VerticalOffset + textBox.ViewportHeight)) < (textBox.FontSize * 3);
                textBox.Text = string.Join(Environment.NewLine, visibleLines);
                if (doScroll)
                    textBox.ScrollToEnd();
                updateQueued = false;
            }
        }

        // Optimized path to avoid excess allocations and calls to Write(char)
        public override void WriteLine(string value)
        {
            lock (writeLock)
            {
                currentLine.Append(value);
                FlushLine();
            }
        }

        public override void Write(char value)
        {
            lock (writeLock)
            {
                foreach (char c in CoreNewLine)
                {
                    if (value == c)
                    {
                        FlushLine();
                        return;
                    }
                }

                currentLine.Append(value);
            }
        }

        public override void Flush()
        {
            lock (writeLock)
            {
                FlushLine();
            }
        }

        private void FlushLine()
        {
            // Ignore empty lines or duplicate updates
            if (currentLine.Length < 1)
                return;

            visibleLines.Enqueue(currentLine.ToString());
            while (visibleLines.Count > MaxLines)
                visibleLines.Dequeue();

            currentLine.Clear();

            // Don't queue multiple updates at once
            if (!updateQueued)
            {
                textBox.Dispatcher.BeginInvoke(DispatcherPriority.Background, updateText);
                updateQueued = true;
            }
        }
    }
}
