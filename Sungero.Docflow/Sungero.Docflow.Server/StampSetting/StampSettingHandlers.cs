using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.StampSetting;

namespace Sungero.Docflow
{
  partial class StampSettingServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.Status == Status.Active)
      {
        var duplicatesError = Functions.StampSetting.GetDuplicatesErrorText(_obj);
        if (!string.IsNullOrEmpty(duplicatesError))
          e.AddError(duplicatesError, _obj.Info.Actions.ShowDuplicates);
      }
      
      // Оптимизация. При изменении логотипа преобразовать его в строку Base64, чтобы формирование отметок проходило быстрее.
      if (_obj.State.Properties.Logo.IsChanged)
        _obj.LogoAsBase64 = _obj.Logo != null ? Convert.ToBase64String(_obj.Logo) : null;
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      if (!_obj.State.IsCopied)
      {
        _obj.NeedShowDateTime = false;
        _obj.Title = Resources.SignatureStampSampleTitle.ToString().Replace("<br>", string.Empty);
        _obj.Logo = Convert.FromBase64String(Resources.SignatureStampSampleLogo);
      }
    }
  }

}