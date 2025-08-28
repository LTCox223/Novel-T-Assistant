using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Novel_T_Assistant.Models;

namespace Novel_T_Assistant.ViewModels
{
    public class CharacterViewModel : INotifyPropertyChanged
    {
        private Novel_T_Assistant.Models.Character _character;

        public CharacterViewModel()
        {
            _character = new Novel_T_Assistant.Models.Character();
            Aliases = new ObservableCollection<string>();
            Tags = new ObservableCollection<string>();

            // For UI - temporary input fields
            NewAlias = "";
            NewTag = "";
        }

        public CharacterViewModel(Novel_T_Assistant.Models.Character character)
        {
            _character = character;

            // Convert lists to observable collections for UI
            Aliases = new ObservableCollection<string>(_character.Aliases);
            Tags = new ObservableCollection<string>(_character.Tags);

            NewAlias = "";
            NewTag = "";
        }

        // Properties bound to UI
        public string Id => _character.Id;

        public string Name
        {
            get => _character.Name;
            set
            {
                if (_character.Name != value)
                {
                    _character.Name = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> Aliases { get; }
        public ObservableCollection<string> Tags { get; }

        // Temporary input fields
        private string _newAlias;
        public string NewAlias
        {
            get => _newAlias;
            set
            {
                if (_newAlias != value)
                {
                    _newAlias = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _newTag;
        public string NewTag
        {
            get => _newTag;
            set
            {
                if (_newTag != value)
                {
                    _newTag = value;
                    OnPropertyChanged();
                }
            }
        }

        // Commands for adding items
        public void AddAlias()
        {
            if (!string.IsNullOrWhiteSpace(NewAlias))
            {
                Aliases.Clear();
                char[] delimiters = { ',', ' ' };
                string[] strings = NewAlias.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in strings)
                {
                    Aliases.Add(s);
                    _character.Aliases.Add(s);
                }
                NewAlias = "";
            }
        }

        public void RemoveAlias(string alias)
        {
            Aliases.Remove(alias);
            _character.Aliases.Remove(alias);
        }

        public void AddTag()
        {
            if (!string.IsNullOrWhiteSpace(NewTag))
            {
                Tags.Clear();
                string[] strings = NewTag.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in strings)
                {
                    Tags.Add(s);
                    _character.Tags.Add(s);
                }
                NewTag = "";
            }
        }

        public void RemoveTag(string tag)
        {
            Tags.Remove(tag);
            _character.Tags.Remove(tag);
        }

        // Get the underlying character model for saving
        public Novel_T_Assistant.Models.Character GetCharacter()
        {
            // Sync collections back to model
            _character.Aliases = new List<string>(Aliases);
            _character.Tags = new List<string>(Tags);
            return _character;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
