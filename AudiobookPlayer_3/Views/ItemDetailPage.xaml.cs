using AudiobookPlayer_3.ViewModels;
using System.ComponentModel;
using Xamarin.Forms;

namespace AudiobookPlayer_3.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        public ItemDetailPage()
        {
            InitializeComponent();
            BindingContext = new ItemDetailViewModel();
        }
    }
}