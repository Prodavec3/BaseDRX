using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.StampSetting;

namespace Sungero.Docflow.Client
{
  partial class StampSettingFunctions
  {
    /// <summary>
    /// Загрузить логотип организации.
    /// </summary>
    public virtual void UploadLogo()
    {
      if (!_obj.AccessRights.CanUpdate())
      {
        Dialogs.ShowMessage(StampSettings.Resources.NoRightsToChangeLogo);
        return;
      }
      
      const int MaxPhotoSize = 0x10000;
      
      var dialog = Dialogs.CreateInputDialog(StampSettings.Resources.UploadLogoDialogTitle);
      var logo = dialog.AddFileSelect(Sungero.Docflow.StampSettings.Resources.SelectLogo, true);
      logo.MaxFileSize(MaxPhotoSize);
      logo.WithFilter(string.Empty, "png");
      var uploadButton = dialog.Buttons.AddCustom(StampSettings.Resources.UploadLogoDialogButton);
      dialog.Buttons.AddCancel();
      
      if (dialog.Show() == uploadButton && logo.Value != null)
        _obj.Logo = logo.Value.Content;
    }
  }
}