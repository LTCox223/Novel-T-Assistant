using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Novel_T_Assistant.ViewModels;
using Novel_T_Assistant.Services;

namespace Novel_T_Assistant.Views;

public partial class TextEditorView : UserControl
{
    private TextEditorViewModel ViewModel => DataContext as TextEditorViewModel;
    private AutoLinkService _autoLinkService;
    private List<DetectedLink> _currentLinks = new List<DetectedLink>();
    private System.Timers.Timer _linkDetectionTimer;

    public TextEditorView()
    {
        InitializeComponent();
        DataContext = new TextEditorViewModel();

        // Initialize auto-link service
        _autoLinkService = new AutoLinkService();
        InitializeLinkDetection();

        // Set initial focus to editor
        MainEditor.Focus();
    }

    private void InitializeLinkDetection()
    {
        // Set up a timer to detect links after user stops typing
        _linkDetectionTimer = new System.Timers.Timer(500); // 500ms delay
        _linkDetectionTimer.AutoReset = false;
        _linkDetectionTimer.Elapsed += async (s, e) =>
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                DetectAndHighlightLinks();
            });
        };
    }

    private void DetectAndHighlightLinks()
    {
        if (ViewModel == null || string.IsNullOrEmpty(MainEditor.Text))
            return;

        // Detect links in the current text
        _currentLinks = _autoLinkService.DetectLinks(MainEditor.Text);

        // Update status to show link count
        if (_currentLinks.Count > 0)
        {
            ViewModel.StatusMessage = $"Found {_currentLinks.Count} linkable references";
        }
        else
        {
            ViewModel.StatusMessage = $"Found no linkable references";
        }

        // TODO: Apply visual highlighting to detected links
        // This would require a more advanced text control or custom rendering
    }

    // File Operations
    private async void OnNewDocument(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        // Check if current document needs saving
        if (ViewModel.IsModified)
        {
            var result = await ShowSaveConfirmationDialog();
            if (result == true)
            {
                await SaveDocument();
            }
        }

        // Clear the editor
        ViewModel.NewDocument();
        MainEditor.Clear();
    }

    private async void OnOpenDocument(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Document",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Text Documents")
                {
                    Patterns = new[] { "*.txt", "*.md", "*.rtf" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (files.Count > 0)
        {
            var file = files[0];
            using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            MainEditor.Text = content;
            ViewModel.DocumentContent = content;
            ViewModel.CurrentFilePath = file.Path.LocalPath;
            ViewModel.DocumentTitle = Path.GetFileNameWithoutExtension(file.Name);
            ViewModel.IsModified = false;
        }
    }

    private async void OnSaveDocument(object sender, RoutedEventArgs e)
    {
        await SaveDocument();
    }

    private async Task<bool> SaveDocument()
    {
        if (string.IsNullOrEmpty(ViewModel.CurrentFilePath))
        {
            return await SaveDocumentAs();
        }

        try
        {
            await File.WriteAllTextAsync(ViewModel.CurrentFilePath, MainEditor.Text);
            ViewModel.IsModified = false;
            ViewModel.LastSaved = DateTime.Now;
            ViewModel.StatusMessage = "Document saved successfully";
            return true;
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Failed to save document: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> SaveDocumentAs()
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Document",
            DefaultExtension = "txt",
            SuggestedFileName = string.IsNullOrEmpty(ViewModel.DocumentTitle) ? "Untitled" : ViewModel.DocumentTitle,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Text File")
                {
                    Patterns = new[] { "*.txt" }
                },
                new FilePickerFileType("Markdown")
                {
                    Patterns = new[] { "*.md" }
                }
            }
        });

        if (file != null)
        {
            using var stream = await file.OpenWriteAsync();
            using var writer = new StreamWriter(stream);
            await writer.WriteAsync(MainEditor.Text);

            ViewModel.CurrentFilePath = file.Path.LocalPath;
            ViewModel.IsModified = false;
            ViewModel.LastSaved = DateTime.Now;
            return true;
        }

        return false;
    }

    // Export Functions
    private async void OnExportRtf(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export as RTF",
            DefaultExtension = "rtf",
            SuggestedFileName = string.IsNullOrEmpty(ViewModel.DocumentTitle) ? "Untitled" : ViewModel.DocumentTitle,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Rich Text Format")
                {
                    Patterns = new[] { "*.rtf" }
                }
            }
        });

        if (file != null)
        {
            try
            {
                // Use auto-link service to export with detected links
                string rtfContent;
                if (_currentLinks != null && _currentLinks.Count > 0)
                {
                    rtfContent = _autoLinkService.ConvertToRtfWithLinks(MainEditor.Text,MainEditor.Name, _currentLinks);
                }
                else
                {
                    rtfContent = ConvertToRtf(MainEditor.Text, ViewModel.DocumentTitle);
                }

                using var stream = await file.OpenWriteAsync();
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync(rtfContent);

                ViewModel.StatusMessage = $"Exported to RTF with {_currentLinks?.Count ?? 0} links: {file.Name}";
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Failed to export RTF: {ex.Message}");
            }
        }
    }

    private string ConvertToRtf(string plainText, string title)
    {
        var rtf = new StringBuilder();

        // RTF Header
        rtf.AppendLine(@"{\rtf1\ansi\deff0 {\fonttbl{\f0 Times New Roman;}}");
        rtf.AppendLine(@"{\colortbl;\red0\green0\blue0;}");
        rtf.AppendLine(@"\viewkind4\uc1\pard\lang1033\f0\fs24");

        // Document title (if provided)
        if (!string.IsNullOrEmpty(title))
        {
            rtf.AppendLine(@"\qc\b\fs32 " + EscapeRtf(title) + @"\b0\fs24\par");
            rtf.AppendLine(@"\par");
        }

        // Convert plain text to RTF
        var lines = plainText.Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                rtf.AppendLine(@"\par");
            }
            else
            {
                // Check for basic formatting patterns
                var processedLine = ProcessLineForRtf(line);
                rtf.AppendLine(@"\ql " + processedLine + @"\par");
            }
        }

        rtf.AppendLine("}");

        return rtf.ToString();
    }

    private string ProcessLineForRtf(string line)
    {
        var processed = EscapeRtf(line);

        // Convert markdown-style headers to RTF
        if (processed.StartsWith("# "))
        {
            processed = @"\b\fs32 " + processed.Substring(2) + @"\b0\fs24";
        }
        else if (processed.StartsWith("## "))
        {
            processed = @"\b\fs28 " + processed.Substring(3) + @"\b0\fs24";
        }
        else if (processed.StartsWith("### "))
        {
            processed = @"\b\fs24 " + processed.Substring(4) + @"\b0\fs24";
        }

        // Convert **bold** to RTF bold
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"\*\*(.+?)\*\*",
            @"\b $1\b0 "
        );

        // Convert *italic* to RTF italic
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"\*(.+?)\*",
            @"\i $1\i0 "
        );

        return processed;
    }

    private string EscapeRtf(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace("\r", "");
    }

    private async void OnExportHtml(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export as HTML",
            DefaultExtension = "html",
            SuggestedFileName = string.IsNullOrEmpty(ViewModel.DocumentTitle) ? "Untitled" : ViewModel.DocumentTitle,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("HTML Document")
                {
                    Patterns = new[] { "*.html", "*.htm" }
                }
            }
        });

        if (file != null)
        {
            try
            {
                var htmlContent = ConvertToHtml(MainEditor.Text, ViewModel.DocumentTitle);
                using var stream = await file.OpenWriteAsync();
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync(htmlContent);

                ViewModel.StatusMessage = $"Exported to HTML: {file.Name}";
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Failed to export HTML: {ex.Message}");
            }
        }
    }

    private string ConvertToHtml(string plainText, string title)
    {
        var html = new StringBuilder();

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine($"<title>{System.Web.HttpUtility.HtmlEncode(title ?? "Document")}</title>");
        html.AppendLine("<meta charset=\"UTF-8\">");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: 'Times New Roman', serif; line-height: 1.6; max-width: 800px; margin: 0 auto; padding: 20px; }");
        html.AppendLine("h1 { text-align: center; }");
        html.AppendLine("p { text-indent: 2em; margin: 1em 0; }");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");

        if (!string.IsNullOrEmpty(title))
        {
            html.AppendLine($"<h1>{System.Web.HttpUtility.HtmlEncode(title)}</h1>");
        }

        var lines = plainText.Split('\n');
        var inParagraph = false;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (inParagraph)
                {
                    html.AppendLine("</p>");
                    inParagraph = false;
                }
            }
            else
            {
                if (!inParagraph)
                {
                    html.Append("<p>");
                    inParagraph = true;
                }
                html.Append(System.Web.HttpUtility.HtmlEncode(line) + " ");
            }
        }

        if (inParagraph)
        {
            html.AppendLine("</p>");
        }

        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    private async void OnExportMarkdown(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export as Markdown",
            DefaultExtension = "md",
            SuggestedFileName = string.IsNullOrEmpty(ViewModel.DocumentTitle) ? "Untitled" : ViewModel.DocumentTitle,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Markdown")
                {
                    Patterns = new[] { "*.md" }
                }
            }
        });

        if (file != null)
        {
            try
            {
                var markdownContent = ConvertToMarkdown(MainEditor.Text, ViewModel.DocumentTitle);
                using var stream = await file.OpenWriteAsync();
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync(markdownContent);

                ViewModel.StatusMessage = $"Exported to Markdown: {file.Name}";
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Failed to export Markdown: {ex.Message}");
            }
        }
    }

    private string ConvertToMarkdown(string plainText, string title)
    {
        var markdown = new StringBuilder();

        if (!string.IsNullOrEmpty(title))
        {
            markdown.AppendLine($"# {title}");
            markdown.AppendLine();
        }

        markdown.Append(plainText);

        return markdown.ToString();
    }

    // Format Controls
    private void OnBoldClick(object sender, RoutedEventArgs e)
    {
        // In a real implementation, you'd apply formatting to selected text
        // For now, we'll add markdown-style formatting
        WrapSelection("**", "**");
    }

    private void OnItalicClick(object sender, RoutedEventArgs e)
    {
        WrapSelection("*", "*");
    }

    private void OnUnderlineClick(object sender, RoutedEventArgs e)
    {
        WrapSelection("<u>", "</u>");
    }

    private void WrapSelection(string prefix, string suffix)
    {
        var start = MainEditor.SelectionStart;
        var length = MainEditor.SelectionEnd - MainEditor.SelectionStart;

        if (length > 0)
        {
            var selectedText = MainEditor.Text.Substring(start, length);
            var wrappedText = prefix + selectedText + suffix;

            MainEditor.Text = MainEditor.Text.Remove(start, length).Insert(start, wrappedText);
            MainEditor.SelectionStart = start;
            MainEditor.SelectionEnd = start + wrappedText.Length;
        }
        else
        {
            MainEditor.Text = MainEditor.Text.Insert(start, prefix + suffix);
            MainEditor.CaretIndex = start + prefix.Length;
        }

        MainEditor.Focus();
    }

    // Alignment
    private void OnAlignLeft(object sender, RoutedEventArgs e) { /* TODO */ }
    private void OnAlignCenter(object sender, RoutedEventArgs e) { /* TODO */ }
    private void OnAlignRight(object sender, RoutedEventArgs e) { /* TODO */ }
    private void OnAlignJustify(object sender, RoutedEventArgs e) { /* TODO */ }

    // Lists
    private void OnBulletList(object sender, RoutedEventArgs e)
    {
        InsertAtLineStart("• ");
    }

    private void OnNumberList(object sender, RoutedEventArgs e)
    {
        InsertAtLineStart("1. ");
    }

    private void InsertAtLineStart(string text)
    {
        var caretIndex = MainEditor.CaretIndex;
        var lines = MainEditor.Text.Substring(0, caretIndex).Split('\n');
        var currentLineStart = caretIndex - (lines.Length > 0 ? lines[lines.Length - 1].Length : 0);

        MainEditor.Text = MainEditor.Text.Insert(currentLineStart, text);
        MainEditor.CaretIndex = caretIndex + text.Length;
        MainEditor.Focus();
    }

    // Indent
    private void OnDecreaseIndent(object sender, RoutedEventArgs e) { /* TODO */ }
    private void OnIncreaseIndent(object sender, RoutedEventArgs e) { /* TODO */ }

    // Special Functions
    private async void OnFindReplace(object sender, RoutedEventArgs e)
    {
        // TODO: Implement find/replace dialog
        ViewModel.StatusMessage = "Find & Replace coming soon";
    }

    private async void OnInsertLink(object sender, RoutedEventArgs e)
    {
        // Show a quick reference panel with detected entities
        var selectedText = MainEditor.SelectedText;

        if (!string.IsNullOrEmpty(selectedText))
        {
            // Check if selected text matches any entity
            var entity = _autoLinkService.GetEntityById(selectedText);
            if (entity != null)
            {
                // Show entity details in a tooltip or side panel
                ViewModel.StatusMessage = $"Linked to {entity.Type}: {entity.Name}";
            }
            else
            {
                // Offer to create new entity with this name
                ViewModel.StatusMessage = $"No entity found for '{selectedText}'. Create new?";
            }
        }
        else
        {
            // Show list of available entities to insert
            ViewModel.StatusMessage = "Select text to link or choose from entity list";
        }

        MainEditor.Focus();
    }

    // Zoom Controls
    private void OnZoomIn(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null && ViewModel.ZoomLevel < 200)
        {
            ViewModel.ZoomLevel += 10;
            MainEditor.FontSize = 14 * (ViewModel.ZoomLevel / 100.0);
        }
    }

    private void OnZoomOut(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null && ViewModel.ZoomLevel > 50)
        {
            ViewModel.ZoomLevel -= 10;
            MainEditor.FontSize = 14 * (ViewModel.ZoomLevel / 100.0);
        }
    }

    // Editor Events
    private void OnEditorPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (ViewModel != null)
        {
            // Check if the selection properties changed
            if (e.Property.Name == "SelectionStart" ||
                e.Property.Name == "SelectionEnd" ||
                e.Property.Name == "Text")
            {
                // Update word count
                UpdateWordCount();

                // Restart link detection timer when text changes
                if (e.Property.Name == "Text")
                {
                    _linkDetectionTimer?.Stop();
                    _linkDetectionTimer?.Start();
                }
            }
        }
    }

    private void OnEditorKeyDown(object sender, KeyEventArgs e)
    {
        if (ViewModel == null) return;

        // Mark document as modified
        if (!ViewModel.IsModified &&
            e.Key != Key.LeftCtrl &&
            e.Key != Key.RightCtrl &&
            e.Key != Key.LeftAlt &&
            e.Key != Key.RightAlt &&
            e.Key != Key.LeftShift &&
            e.Key != Key.RightShift)
        {
            ViewModel.IsModified = true;
        }

        // Handle keyboard shortcuts
        if (e.KeyModifiers == KeyModifiers.Control)
        {
            switch (e.Key)
            {
                case Key.S:
                    OnSaveDocument(sender, e);
                    e.Handled = true;
                    break;
                case Key.O:
                    OnOpenDocument(sender, e);
                    e.Handled = true;
                    break;
                case Key.N:
                    OnNewDocument(sender, e);
                    e.Handled = true;
                    break;
                case Key.B:
                    OnBoldClick(sender, e);
                    e.Handled = true;
                    break;
                case Key.I:
                    OnItalicClick(sender, e);
                    e.Handled = true;
                    break;
                case Key.U:
                    OnUnderlineClick(sender, e);
                    e.Handled = true;
                    break;
                case Key.F:
                    OnFindReplace(sender, e);
                    e.Handled = true;
                    break;
            }
        }

        // Auto-save trigger
        if (ViewModel.AutoSaveEnabled)
        {
            RestartAutoSaveTimer();
        }
    }

    private void UpdateWordCount()
    {
        if (ViewModel == null) return;

        var text = MainEditor.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            ViewModel.WordCount = 0;
            ViewModel.CharCount = 0;
            return;
        }

        // Count words
        var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        ViewModel.WordCount = words.Length;

        // Count characters (excluding spaces)
        ViewModel.CharCount = text.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "").Length;
    }

    // Auto-save functionality
    private System.Timers.Timer _autoSaveTimer;

    private void RestartAutoSaveTimer()
    {
        _autoSaveTimer?.Stop();
        _autoSaveTimer?.Dispose();

        _autoSaveTimer = new System.Timers.Timer(30000); // 30 seconds
        _autoSaveTimer.Elapsed += async (s, e) =>
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (ViewModel != null && ViewModel.IsModified && !string.IsNullOrEmpty(ViewModel.CurrentFilePath))
                {
                    await SaveDocument();
                    ViewModel.StatusMessage = "Auto-saved";
                }
            });
        };
        _autoSaveTimer.Start();
    }

    // Helper Dialogs
    private async Task<bool?> ShowSaveConfirmationDialog()
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null) return false;

        var dialog = new Window
        {
            Title = "Save Changes?",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Border
            {
                Padding = new Thickness(20),
                Child = new StackPanel
                {
                    Spacing = 20,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Do you want to save changes to your document?",
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                        },
                        new StackPanel
                        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Spacing = 10,
                            Children =
                            {
                                new Button { Content = "Save", Width = 80 },
                                new Button { Content = "Don't Save", Width = 80 },
                                new Button { Content = "Cancel", Width = 80 }
                            }
                        }
                    }
                }
            }
        };

        var buttons = ((dialog.Content as Border).Child as StackPanel).Children[1] as StackPanel;
        var saveBtn = buttons.Children[0] as Button;
        var dontSaveBtn = buttons.Children[1] as Button;
        var cancelBtn = buttons.Children[2] as Button;

        bool? result = null;
        saveBtn.Click += (s, e) => { result = true; dialog.Close(); };
        dontSaveBtn.Click += (s, e) => { result = false; dialog.Close(); };
        cancelBtn.Click += (s, e) => { result = null; dialog.Close(); };

        await dialog.ShowDialog(window);
        return result;
    }

    private async Task ShowErrorDialog(string message)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null) return;

        var dialog = new Window
        {
            Title = "Error",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Border
            {
                Padding = new Thickness(20),
                Child = new StackPanel
                {
                    Spacing = 20,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                        },
                        new Button
                        {
                            Content = "OK",
                            Width = 80,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                        }
                    }
                }
            }
        };

        var okButton = ((dialog.Content as Border).Child as StackPanel).Children[1] as Button;
        okButton.Click += (s, e) => dialog.Close();

        await dialog.ShowDialog(window);
    }
}