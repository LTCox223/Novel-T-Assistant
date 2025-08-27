using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Novel_T_Assistant.Models
{
    public class Character
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public List<string> Aliases { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
    }
}
