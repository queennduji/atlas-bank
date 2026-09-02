using CommunityToolkit.Mvvm.ComponentModel;

namespace AtlasBank.Maui.ViewModels;

/// <summary>Common busy/error state every screen in this app needs – a loading spinner while
/// a request is in flight, and a dismissible banner when one fails.</summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    public bool IsNotBusy => !IsBusy;

    /// <summary>Toggles IsBusy around an async operation and catches
    /// <see cref="AtlasBank.Clients.Core.Api.ApiException"/>/<see cref="AtlasBank.Clients.Core.Auth.AuthException"/>
    /// into <see cref="ErrorMessage"/> instead of letting them crash the page. Every command
    /// in this app goes through this.</summary>
    protected async Task RunAsync(Func<Task> operation)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            // no ConfigureAwait(false) here on purpose – the code after this await updates
            // bound properties, and it needs to land back on the UI thread's
            // SynchronizationContext to do that safely, same as it would in WPF
            await operation();
        }
        catch (AtlasBank.Clients.Core.Api.ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (AtlasBank.Clients.Core.Auth.AuthException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Couldn't reach Atlas Bank. Check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
