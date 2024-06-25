using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pickshop_Finalss
{
    public class User
    {
        public string Name { get; }
        public string Email { get; }

        public string Number { get; }

        public User(/*string imagePath,*/string name, string email, string number)
        {
            //ImagePath = imagePath;
            Name = name;
            Email = email;
            Number = number;
        }
    }
}
