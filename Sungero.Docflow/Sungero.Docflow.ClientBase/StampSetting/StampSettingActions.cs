using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.StampSetting;

namespace Sungero.Docflow.Client
{
  partial class StampSettingActions
  {
    public virtual void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var duplicates = Functions.StampSetting.GetDuplicates(_obj);
      if (duplicates.Any())
        duplicates.Show();
      else
        Dialogs.NotifyMessage(StampSettings.Resources.DuplicatesNotFound);
    }

    public virtual bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void DeleteLogo(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.Logo != null)
        _obj.Logo = null;
    }

    public virtual bool CanDeleteLogo(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate() && _obj.Logo != null;
    }

    public virtual void UploadLogo(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.StampSetting.UploadLogo(_obj);
    }

    public virtual bool CanUploadLogo(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate();
    }

  }

}