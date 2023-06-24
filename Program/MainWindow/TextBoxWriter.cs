using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// A text writer which redirects the standard output stream to a WPF textbox.
    /// </summary>
    /// <remarks>
    /// https://social.technet.microsoft.com/wiki/contents/articles/12347.wpfhowto-add-a-debugoutput-console-to-your-application.aspx
    /// </remarks>
    public class TextBoxWriter : TextWriter
    {
        /// <summary>
        /// Default maximum number of lines to keep in console.
        /// </summary>
        private const int DefaultMaxNumberLines = 100;

        /// <summary>
        /// Line split characters.
        /// </summary>
        private static readonly char[] newlineChars = Environment.NewLine.ToCharArray();

        /// <summary>
        /// Cache for current line.
        /// </summary>
        private static StringBuilder currentLineCache = null;

        /// <summary>
        /// Cache of the visible text.
        /// </summary>
        private static FixedSizeConcurrentQueue<string> visibleTextCache = null;

        /// <summary>
        /// Text box handle that displays text.
        /// </summary>
        private readonly TextBox textBox = null;

        /// <summary>
        /// Display lines in reverse order (newest first) if set. Defaults to false.
        /// </summary>
        private readonly bool displayLinesInReverseOrder = false;

        /// <summary>
        /// Display timestamp for each line. Defaults to true.
        /// </summary>
        private readonly bool displayLinesWithTimestamp = true;

        /// <summary>
        /// Connects console output to a given text box.
        /// </summary>
        public static void RedirectConsoleToTextBox(
            TextBox textBox,
            int maxNumberOfLines = DefaultMaxNumberLines,
            bool displayLinesInReverseOrder = false,
            bool displayLinesWithTimestamp = true
        )
        {
            var textboxConsole = new TextBoxWriter(textBox, displayLinesInReverseOrder, displayLinesWithTimestamp);
            Console.SetOut(textboxConsole);
            currentLineCache = new StringBuilder();
            visibleTextCache = new FixedSizeConcurrentQueue<string>(maxNumberOfLines);
        }

        /// <summary>
        /// Creates a new TextBoxConsole.
        /// </summary>
        public TextBoxWriter(TextBox output, bool reverse = false, bool timestamp = true)
        {
            textBox = output;
            displayLinesInReverseOrder = reverse;
            displayLinesWithTimestamp = timestamp;
        }

        /// <summary>
        /// Write text to outputter.
        /// </summary>
        public override void Write(char value)
        {
            base.Write(value);
            if (textBox != null &&
                currentLineCache != null &&
                visibleTextCache != null)
            {
                textBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (newlineChars.Contains(value))
                    {
                        // Newline
                        if (currentLineCache.Length > 0)
                        {
                            // Store line in text cache and flush it
                            string newText = (displayLinesWithTimestamp) ? "[" + DateTime.Now.ToString("s") + "] " : string.Empty;
                            newText += currentLineCache.ToString();
                            visibleTextCache.Enqueue(newText);
                            currentLineCache.Clear();

                            // Display text cache
                            if (displayLinesInReverseOrder)
                            {
                                textBox.Text = string.Join(Environment.NewLine, visibleTextCache.ToArray().Reverse());
                            }
                            else
                            {
                                textBox.Text = string.Join(Environment.NewLine, visibleTextCache.ToArray());
                            }
                        }
                    }
                    else
                    {
                        // Collect characters in line
                        currentLineCache.Append(value);
                    }
                }));
            }
        }

        /// <summary>
        /// Gets the encoding of the outputter.
        /// </summary>
        public override Encoding Encoding => Encoding.UTF8;
    }
}
