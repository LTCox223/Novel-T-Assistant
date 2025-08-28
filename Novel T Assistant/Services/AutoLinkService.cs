using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Novel_T_Assistant.Models;

namespace Novel_T_Assistant.Services;

public class AutoLinkService
{
    private List<LinkableEntity> _entities = new List<LinkableEntity>();
    private string _dataDirectory = Path.Combine("data");

    public AutoLinkService()
    {
        LoadEntities();
    }

    /// <summary>
    /// Load all entities from JSON files in the data directory
    /// </summary>
    public async Task LoadEntities()
    {
        _entities.Clear();

        // Load characters
        var charactersDir = Path.Combine(_dataDirectory, "characters");
        if (Directory.Exists(charactersDir))
        {
            foreach (var file in Directory.GetFiles(charactersDir, "*.json"))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var character = JsonSerializer.Deserialize<Character>(json);
                    if (character != null)
                    {
                        var entity = new LinkableEntity
                        {
                            Id = character.Id,
                            Name = character.Name,
                            Type = EntityType.Character,
                            Aliases = character.Aliases,
                            FilePath = file
                        };

                        _entities.Add(entity);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading character file {file}: {ex.Message}");
                }
            }
        }

        // TODO: Load locations, events, items from their respective directories
    }

    /// <summary>
    /// Process text and find all linkable entities
    /// Case-sensitive, whole word matching only
    /// </summary>
    public List<DetectedLink> DetectLinks(string text)
    {
        var detectedLinks = new List<DetectedLink>();

        if (string.IsNullOrWhiteSpace(text))
            return detectedLinks;

        // Build a list of all searchable terms with their entities
        var searchTerms = new List<(string term, LinkableEntity entity)>();

        foreach (var entity in _entities)
        {
            // Add main name
            if (!string.IsNullOrWhiteSpace(entity.Name))
            {
                searchTerms.Add((entity.Name, entity));
            }

            // Add all aliases
            if (entity.Aliases != null)
            {
                foreach (var alias in entity.Aliases)
                {
                    searchTerms.Add((alias, entity));
                }
            }
        }

        // Sort by length (longest first) to match "John Smith" before "John"
        searchTerms = searchTerms.OrderByDescending(st => st.term.Length).ToList();

        // Find all matches
        foreach (var (term, entity) in searchTerms)
        {
            // Create pattern for exact word match
            // \b ensures word boundaries - won't match ARM in ARMADA
            var pattern = $@"\b{Regex.Escape(term)}\b";

            // Case-sensitive matching
            var matches = Regex.Matches(text, pattern, RegexOptions.None);

            foreach (Match match in matches)
            {
                // Check if this position overlaps with an already detected link
                bool overlaps = detectedLinks.Any(link =>
                    match.Index < link.EndIndex && match.Index + match.Length > link.StartIndex);

                if (!overlaps)
                {
                    detectedLinks.Add(new DetectedLink
                    {
                        Entity = entity,
                        StartIndex = match.Index,
                        EndIndex = match.Index + match.Length,
                        MatchedText = match.Value
                    });
                }
            }
        }

        return detectedLinks.OrderBy(l => l.StartIndex).ToList();
    }

    /// <summary>
    /// Get entity by exact name or alias match
    /// </summary>
    public LinkableEntity GetEntityByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return _entities.FirstOrDefault(e =>
            e.Name == name ||
            e.Aliases.Contains(name));
    }

    /// <summary>
    /// Get entity by ID
    /// </summary>
    public LinkableEntity GetEntityById(string id)
    {
        return _entities.FirstOrDefault(e => e.Id == id);
    }

    /// <summary>
    /// Load the full content of an entity (from markdown file if available)
    /// </summary>
    public async Task<string> LoadEntityContent(LinkableEntity entity)
    {
        if (entity == null || string.IsNullOrEmpty(entity.FilePath))
            return null;

        // Try to find corresponding markdown file
        var jsonPath = entity.FilePath;
        var mdPath = Path.ChangeExtension(jsonPath, ".md");

        if (File.Exists(mdPath))
        {
            return await File.ReadAllTextAsync(mdPath);
        }

        return null;
    }

    /// <summary>
    /// Export text with links formatted for RTF
    /// </summary>
    public string ConvertToRtfWithLinks(string text, string title, List<DetectedLink> links)
    {
        var rtf = new StringBuilder();

        // RTF Header
        rtf.AppendLine(@"{\rtf1\ansi\deff0 {\fonttbl{\f0 Times New Roman;}}");
        rtf.AppendLine(@"{\colortbl;\red0\green0\blue0;\red0\green0\blue255;}"); // Black and blue
        rtf.AppendLine(@"\viewkind4\uc1\pard\lang1033\f0\fs24");

        // Document title
        if (!string.IsNullOrEmpty(title))
        {
            rtf.AppendLine(@"\qc\b\fs32 " + EscapeRtf(title) + @"\b0\fs24\par");
            rtf.AppendLine(@"\par");
        }

        // Process text with links
        rtf.Append(@"\ql ");

        int lastIndex = 0;
        foreach (var link in links.OrderBy(l => l.StartIndex))
        {
            // Add text before the link
            if (link.StartIndex > lastIndex)
            {
                var beforeText = text.Substring(lastIndex, link.StartIndex - lastIndex);
                rtf.Append(EscapeRtf(beforeText));
            }

            // Add the linked text (blue and underlined)
            rtf.Append(@"{\cf2\ul ");
            rtf.Append(EscapeRtf(link.MatchedText));
            rtf.Append(@"\cf0\ulnone}");

            lastIndex = link.EndIndex;
        }

        // Add remaining text
        if (lastIndex < text.Length)
        {
            rtf.Append(EscapeRtf(text.Substring(lastIndex)));
        }

        rtf.AppendLine(@"\par}");
        return rtf.ToString();
    }

    private string EscapeRtf(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        return text
            .Replace("\\", "\\\\")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace("\r\n", @"\par ")
            .Replace("\n", @"\par ")
            .Replace("\r", @"\par ");
    }

    /// <summary>
    /// Reload entities (useful after adding new characters/locations)
    /// </summary>
    public async Task RefreshEntities()
    {
        await LoadEntities();
    }
}

// Models for the linking system
public class LinkableEntity
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<string> Aliases { get; set; } = new List<string>();
    public EntityType Type { get; set; }
    public string FilePath { get; set; }

    // Quick access to basic info from JSON
    public List<string> Tags { get; set; } = new List<string>();
}

public class DetectedLink
{
    public LinkableEntity Entity { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public string MatchedText { get; set; }
}

public enum EntityType
{
    Character,
    Location,
    Event,
    Item,
    Custom
}