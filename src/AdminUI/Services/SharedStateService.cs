namespace AzUrlShortener.AdminUI.Services
{
    public class SharedStateService
    {
        private bool _showOnlyMyRecords = false;
        public bool ShowOnlyMyRecords
        {
            get => _showOnlyMyRecords;
            set => _showOnlyMyRecords = value;
        }
    }
}
