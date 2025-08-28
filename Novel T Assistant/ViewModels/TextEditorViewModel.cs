using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ReactiveUI;

namespace Novel_T_Assistant.ViewModels;

public class TextEditorViewModel : ViewModelBase, INotifyPropertyChanged
{
    private string _documentTitle = "Untitled";
    private string _documentContent = "";
    private string _currentFilePath = "";
    private bool _isModified = false;
    private int _wordCount = 0;
    private int _charCount = 0;
    private string _statusMessage = "Ready";
    private DateTime? _lastSaved;
    private bool _autoSaveEnabled = true;
    private int _zoomLevel = 100;

    public TextEditorViewModel()
    {
        // Initialize with empty document
        NewDocument();
    }

    public string DocumentTitle
    {
        get => _documentTitle;
        set
        {
            if (_documentTitle != value)
            {
                _documentTitle = value;
                OnPropertyChanged();
                IsModified = true;
            }
        }
    }

    public string DocumentContent
    {
        get => _documentContent;
        set
        {
            if (_documentContent != value)
            {
                _documentContent = value;
                OnPropertyChanged();
                UpdateStatistics();
                IsModified = true;
            }
        }
    }

    public string CurrentFilePath
    {
        get => _currentFilePath;
        set
        {
            if (_currentFilePath != value)
            {
                _currentFilePath = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsModified
    {
        get => _isModified;
        set
        {
            if (_isModified != value)
            {
                _isModified = value;
                OnPropertyChanged();
                UpdateStatusMessage();
            }
        }
    }

    public int WordCount
    {
        get => _wordCount;
        set
        {
            if (_wordCount != value)
            {
                _wordCount = value;
                OnPropertyChanged();
            }
        }
    }

    public int CharCount
    {
        get => _charCount;
        set
        {
            if (_charCount != value)
            {
                _charCount = value;
                OnPropertyChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime? LastSaved
    {
        get => _lastSaved;
        set
        {
            if (_lastSaved != value)
            {
                _lastSaved = value;
                OnPropertyChanged();
            }
        }
    }

    public bool AutoSaveEnabled
    {
        get => _autoSaveEnabled;
        set
        {
            if (_autoSaveEnabled != value)
            {
                _autoSaveEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    public int ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            if (_zoomLevel != value)
            {
                _zoomLevel = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ZoomPercentage));
            }
        }
    }

    public string ZoomPercentage => $"{ZoomLevel}%";

    // Methods
    public void NewDocument()
    {
        DocumentTitle = "Untitled";
        DocumentContent = "";
        CurrentFilePath = "";
        IsModified = false;
        LastSaved = null;
        WordCount = 0;
        CharCount = 0;
        StatusMessage = "New document created";
    }

    private void UpdateStatistics()
    {
        if (string.IsNullOrWhiteSpace(DocumentContent))
        {
            WordCount = 0;
            CharCount = 0;
            return;
        }

        // Simple word count
        var words = DocumentContent.Split(new[] { ' ', '\n', '\r', '\t' },
                                         StringSplitOptions.RemoveEmptyEntries);
        WordCount = words.Length;

        // Character count (excluding whitespace)
        CharCount = DocumentContent.Replace(" ", "")
                                  .Replace("\n", "")
                                  .Replace("\r", "")
                                  .Replace("\t", "")
                                  .Length;
    }

    private void UpdateStatusMessage()
    {
        if (IsModified)
        {
            StatusMessage = "Modified";
        }
        else if (LastSaved.HasValue)
        {
            StatusMessage = $"Last saved at {LastSaved.Value:HH:mm:ss}";
        }
        else
        {
            StatusMessage = "Ready";
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// Supporting classes for future enhancements
public class TextFormat
{
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public bool IsUnderline { get; set; }
    public string FontFamily { get; set; } = "Times New Roman";
    public double FontSize { get; set; } = 14;
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;
}

public enum TextAlignment
{
    Left,
    Center,
    Right,
    Justify
}

// Character/Location link model for auto-linking feature
public class EntityLink
{
    public string Id { get; set; }
    public string Name { get; set; }
    public EntityType Type { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
}

public enum EntityType
{
    Character,
    Location,
    Event,
    Item,
    Custom
}