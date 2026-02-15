using ElectronicJova.Models;
using System.ComponentModel.DataAnnotations;

namespace ElectronicJova.Models.ViewModels
{
    public class DetailsVM
    {
        public Product Product { get; set; }

        [Range(1, 1000, ErrorMessage = "Please enter a value between 1 and 1000")]
        public int Count { get; set; }
    }
}
