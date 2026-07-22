using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewsApp.Models
{
    public class Category
    {
        public int Id { get; set; }

        public string NameEnglish { get; set; }

        public string NameBangla { get; set; }

        public ICollection<News>? News { get; set; }
    }
}
