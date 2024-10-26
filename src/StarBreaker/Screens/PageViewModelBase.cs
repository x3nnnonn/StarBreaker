namespace StarBreaker.Screens;

public abstract class PageViewModelBase : ViewModelBase, IPageViewModel
{
    public abstract string Name { get; }
    public abstract string Icon { get; }
}