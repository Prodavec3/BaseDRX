using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.ExchangeCore.Client
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Запуск фонового процесса "Электронный обмен. Синхронизация абонентских ящиков".
    /// </summary>
    public static void SyncBoxes()
    {
      Functions.Module.Remote.RequeueBoxSync();
    }
    
    /// <summary>
    /// Запуск фонового процесса "Электронный обмен. Синхронизация контрагентов".
    /// </summary>
    public static void SyncCounterparties()
    {
      Functions.Module.Remote.RequeueCounterpartySync();
    }
    
    #region Сайт
    
    /// <summary>
    /// Перейти на сайт.
    /// </summary>
    /// <param name="website">Адрес сайта.</param>
    /// <param name="e">Аргумент события.</param>
    public static void GoToWebsite(string website, Sungero.Domain.Client.ExecuteActionArgs e)
    {
      website = website.ToLower();
      if (!(website.StartsWith("http://") || website.StartsWith("https://")))
      {
        website = "http://" + website;
      }

      try
      {
        Hyperlinks.Open(website);
      }
      catch
      {
        e.AddError(Resources.WrongWebsite);
      }
    }

    /// <summary>
    /// Проверить возможность перейти на сайт.
    /// </summary>
    /// <param name="website">Адрес сайта.</param>
    /// <returns>Возможность перейти на сайт.</returns>
    public static bool CanGoToWebsite(string website)
    {
      return !string.IsNullOrWhiteSpace(website);
    }
    
    #endregion
    
  }
}