namespace Cotton.Mobile.ViewModels
{
    public class MainPageProfile
    {
        public MainPageProfile(string name, string? email, string instance)
        {
            Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Profile name is required.", nameof(name)) : name;
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
            Instance = string.IsNullOrWhiteSpace(instance) ? throw new ArgumentException("Profile instance is required.", nameof(instance)) : instance;
        }

        public string Name { get; }

        public string? Email { get; }

        public string Instance { get; }
    }
}
