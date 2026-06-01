namespace Mobile.MAUI
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        public async Task NavigateTo(Page page)
        {
            await Navigation.PushAsync(page);
        }
    }
}
