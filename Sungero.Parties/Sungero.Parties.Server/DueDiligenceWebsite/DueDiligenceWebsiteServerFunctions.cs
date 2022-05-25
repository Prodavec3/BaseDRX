using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.DueDiligenceWebsite;

namespace Sungero.Parties.Server
{
  partial class DueDiligenceWebsiteFunctions
  {
    /// <summary>
    /// Получить сайт проверки контрагента по умолчанию.
    /// </summary>
    /// <returns>Сайт проверки контрагента, используемый по умолчанию.</returns>
    /// <remarks>Если сайт по умолчанию не выбран - передаем сайт проверки, используемый системой по умолчанию по GUID,
    /// Если нет сайта, используемого системой - первый сайт из списка,
    /// Если список пуст - null.</remarks>
    [Remote(IsPure = true), Public]
    public static IDueDiligenceWebsite GetDefaultDueDiligenceWebsite()
    {
      var websites = DueDiligenceWebsites.GetAll();
      var defaultWebsite = websites.FirstOrDefault(s => s.IsDefault == true);
      if (defaultWebsite == null)
      {
        var systemWebsite = GetDefaultSystemDueDiligenceWebsite();
        defaultWebsite = systemWebsite != null ? systemWebsite : websites.FirstOrDefault();
      }
      
      return defaultWebsite;
    }
    
    /// <summary>
    /// Проверить, установлен ли сайт проверки контрагента по умолчанию.
    /// </summary>
    /// <returns>True - если установлен, иначе проверяем наличие сайта, используемого системой по умолчанию.</returns>
    [Remote(IsPure = true), Public]
    public static bool IsDefaultDueDiligenceWebsiteSet()
    {
      var systemWebsite = GetDefaultSystemDueDiligenceWebsite();
      var defaultWebsite = DueDiligenceWebsites.GetAll(s => s.IsDefault == true).FirstOrDefault();
      return defaultWebsite != null ? true : systemWebsite == null;
    }
    
    /// <summary>
    /// Получить сайт проверки контрагента, используемый системой по умолчанию.
    /// </summary>
    /// <returns>Если найден - сайт проверки, иначе - null.</returns>
    public static IDueDiligenceWebsite GetDefaultSystemDueDiligenceWebsite()
    {
      var systemWebsiteGuid = Parties.Constants.DueDiligenceWebsite.Initialize.HonestBusinessWebsite;
      var systemWebsiteExternalLink = Docflow.PublicFunctions.Module.GetExternalLink(DueDiligenceWebsite.ClassTypeGuid, systemWebsiteGuid);
      if (systemWebsiteExternalLink != null && systemWebsiteExternalLink.EntityId.HasValue)
        return DueDiligenceWebsites.Get(systemWebsiteExternalLink.EntityId.Value);
      
      return null;
    }
    
  }
}