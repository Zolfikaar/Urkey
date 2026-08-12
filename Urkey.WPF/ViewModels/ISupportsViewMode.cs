namespace Urkey.WPF.ViewModels
{
    /// <summary>
    /// Marks a page ViewModel that supports list/grid switching via the global header ViewToggler.
    /// </summary>
    public interface ISupportsViewMode
    {
        bool IsListView { get; set; }
        bool IsGridView { get; }
    }
}
