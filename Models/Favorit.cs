using System;

namespace webshop.Models
{

    public class Favorit
    {
        public int UserId { get; set; }
        public User User { get; set; }
        public int ProductId { get; set; }
        public Product product { get; set; }
    }
}