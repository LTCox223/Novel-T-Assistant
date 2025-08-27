using System.IO;
using System.Text.Json;
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Novel_T_Assistant.Models;
using Novel_T_Assistant.ViewModels;

namespace Novel_T_Assistant;

public partial class CharacterView : UserControl
{
    public CharacterView()
    {
        InitializeComponent();
        if (DataContext == null)
        {
            DataContext = new CharacterViewModel();
        }
    }
    private CharacterViewModel ViewModel => DataContext as CharacterViewModel;

    // Add alias when Enter is pressed
    private void OnAliasKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ViewModel?.AddAlias();
        }
    }

    // Add tag when Enter is pressed
    private void OnTagKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ViewModel?.AddTag();
        }
    }

    // Button click handlers
    private void OnAddAliasClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.AddAlias();
    }

    private void OnAddTagClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.AddTag();
    }

    private void OnRemoveAliasClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string alias)
        {
            ViewModel?.RemoveAlias(alias);
        }
    }

    private void OnRemoveTagClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string tag)
        {
            ViewModel?.RemoveTag(tag);
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        // TODO: Navigate back or close dialog
        Console.WriteLine("Cancel clicked");
        
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        // Validate required fields
        if (string.IsNullOrWhiteSpace(ViewModel.Name))
        {
            // TODO: Show validation error
            Console.WriteLine("Name is required");
            return;
        }

        try
        {
            // Get the character data
            var character = ViewModel.GetCharacter();

            // Create directories if they don't exist
            var charactersDir = Path.Combine("data", "characters");
            Directory.CreateDirectory(charactersDir);

            // Save as JSON (for searching/indexing)
            var jsonPath = Path.Combine(charactersDir, $"{character.Name}!{character.Id}.json");
            var jsonContent = JsonSerializer.Serialize(character, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(jsonPath, jsonContent);

            // Save as Markdown (for content/editing)
            var markdownPath = Path.Combine(charactersDir, $"{character.Name}!{character.Id}.md");
            var markdownContent = GenerateMarkdownContent(character);
            await File.WriteAllTextAsync(markdownPath, markdownContent);

            Console.WriteLine($"Character saved: {character.Name} ({character.Id})");

            // TODO: Navigate back to character list or show success message

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving character: {ex.Message}");
            // TODO: Show error dialog
        }
    }

    private string GenerateMarkdownContent(Character character)
    {
        var aliases = character.Aliases.Count > 0 ? $"[{string.Join(", ", character.Aliases)}]" : "[]";
        var tags = character.Tags.Count > 0 ? $"[{string.Join(", ", character.Tags)}]" : "[]";

        return $@"---
id: {character.Id}
type: character
name: {character.Name}
aliases: {aliases}
tags: {tags}
created: {DateTime.Now:yyyy-MM-ddTHH:mm:ssZ}
---

# {character.Name}

## Description
Add character description here...

## Backstory
Add character backstory here...

## Relationships
- Add relationships with other characters using [[Character Name]] links

## Notes
Additional notes about this character...
";
    }
}