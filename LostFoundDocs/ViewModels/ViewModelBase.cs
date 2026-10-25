using System.Reflection;
using LostFoundDocs.Models;

namespace LostFoundDocs.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    private bool _isBusy;

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }
    }

    public bool IsNotBusy => !IsBusy;

    /// <summary>Notifie toutes les propriétés publiques (rechargement complet d'un écran après chargement des données).</summary>
    protected void RaiseAll()
    {
        foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            OnPropertyChanged(property.Name);
        }
    }
}
