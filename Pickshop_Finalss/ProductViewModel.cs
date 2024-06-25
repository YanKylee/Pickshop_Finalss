using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pickshop_Finalss
{
    public class ProductViewModel
    {
        //public string ImagePath { get; }
        public string Category { get; }
        public string Name { get; }
        public string Description { get; }
        public double Price { get; }
        public string Userid { get; }


        public ProductViewModel(/*string imagePath,*/string category, string name, string description, double price, string userid)
        {
            //ImagePath = imagePath;
            Category = category;
            Name = name;
            Description = description;
            Price = price;
            Userid = userid;
        }
    }
}
