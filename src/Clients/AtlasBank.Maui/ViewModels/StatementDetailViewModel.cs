using System.Collections.ObjectModel;
using AtlasBank.Clients.Core.Api;
using AtlasBank.Clients.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AtlasBank.Maui.ViewModels;

[QueryProperty(nameof(StatementId), "statementId")]
public partial class StatementDetailViewModel : ViewModelBase
{
    private readonly AtlasApiClient _api;

    [ObservableProperty]
    private Guid statementId;

    [ObservableProperty]
    private Statement? statement;

    public ObservableCollection<StatementLine> Lines { get; } = [];

    public StatementDetailViewModel(AtlasApiClient api)
    {
        _api = api;
    }

    async partial void OnStatementIdChanged(Guid value)
    {
        if (value != Guid.Empty)
        {
            await RunAsync(async () =>
            {
                Statement = await _api.GetStatementAsync(value);
                Lines.Clear();
                foreach (var line in Statement.Lines)
                {
                    Lines.Add(line);
                }
            });
        }
    }
}
