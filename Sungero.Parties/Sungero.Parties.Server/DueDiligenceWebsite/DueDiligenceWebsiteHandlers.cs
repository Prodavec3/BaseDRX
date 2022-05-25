using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.DueDiligenceWebsite;

namespace Sungero.Parties
{
  partial class DueDiligenceWebsiteServerHandlers
  {
    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.IsDefault = false;
      _obj.IsSystem = false;
    }
    
    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Проверка шаблона сайта.
      if (!string.IsNullOrEmpty(_obj.Url))
      {
        _obj.Url = _obj.Url.Trim();
        var error = Functions.DueDiligenceWebsite.CheckDueDiligenceWebsiteUrl(_obj.Url);
        if (!string.IsNullOrWhiteSpace(error))
          e.AddError(_obj.Info.Properties.Url, error);
      }
      
      // Проверка шаблона сайта (ИП).
      if (!string.IsNullOrEmpty(_obj.UrlForSelfEmployed))
      {
        _obj.UrlForSelfEmployed = _obj.UrlForSelfEmployed.Trim();
        var error = Functions.DueDiligenceWebsite.CheckDueDiligenceWebsiteUrl(_obj.UrlForSelfEmployed);
        if (!string.IsNullOrWhiteSpace(error))
          e.AddError(_obj.Info.Properties.UrlForSelfEmployed, error);
      }
      
      // Если установить для текущего сайта флаг по умолчанию, то с другого сайта этот флаг снимается.
      if (_obj.IsDefault == true)
      {
        var defaultWebsite = Functions.DueDiligenceWebsite.GetDefaultDueDiligenceWebsite();
        if (defaultWebsite != null && !Equals(defaultWebsite, _obj))
        {
          var lockInfo = Locks.GetLockInfo(defaultWebsite);
          if (lockInfo != null && lockInfo.IsLocked)
          {
            var error = Commons.Resources.LinkedEntityLockedFormat(
              defaultWebsite.Name,
              defaultWebsite.Id,
              lockInfo.OwnerName);
            e.AddError(error);
          }
          
          defaultWebsite.IsDefault = false;
        }
      }
      _obj.HomeUrl = _obj.HomeUrl.Trim();
    }
  }
}