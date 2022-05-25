using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.SmartProcessingSetting;
using MessageTypes = Sungero.Docflow.Constants.SmartProcessingSetting.SettingsValidationMessageTypes;

namespace Sungero.Docflow.Client
{
  partial class SmartProcessingSettingActions
  {
    public virtual void Login(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!PublicFunctions.SmartProcessingSetting.Remote.CheckConnection(_obj))
      {
        e.AddError(SmartProcessingSettings.Resources.ArioConnectionError);
        return;
      }
      
      var dialog = Dialogs.CreateInputDialog(SmartProcessingSettings.Resources.LoginActionTitle);
      var password = dialog.AddPasswordString(_obj.Info.Properties.Password.LocalizedName, true);
      password.MaxLength(Constants.SmartProcessingSetting.PasswordMaxLength);
      dialog.Buttons.AddOkCancel();
      dialog.Buttons.Default = DialogButtons.Ok;
      
      dialog.SetOnButtonClick(
        x =>
        {
          if (x.Button == DialogButtons.Ok && x.IsValid)
          {
            var loginResult = Functions.SmartProcessingSetting.Remote.Login(_obj, password.Value, false);
            if (!string.IsNullOrEmpty(loginResult.Error))
            {
              x.AddError(loginResult.Error);
            }
            else
            {
              _obj.Password = loginResult.EncryptedPassword;
              Dialogs.NotifyMessage(SmartProcessingSettings.Resources.ArioConnectionEstablished);
            }
            
          }
        });
      
      dialog.Show();
    }

    public virtual bool CanLogin(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate() && !string.IsNullOrEmpty(_obj.Login) && !string.IsNullOrEmpty(_obj.ArioUrl);
    }

    public virtual void ForceSave(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      e.Params.AddOrUpdate(Constants.SmartProcessingSetting.ForceSaveParamName, true);
      _obj.Save();
    }

    public virtual bool CanForceSave(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void CheckConnection(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Проверка адреса сервиса Ario.
      var arioUrlValidationMessage = Functions.SmartProcessingSetting.ValidateArioUrl(_obj);
      if (arioUrlValidationMessage != null)
      {
        if (arioUrlValidationMessage.Type == MessageTypes.Error)
          e.AddError(arioUrlValidationMessage.Text);
        
        if (arioUrlValidationMessage.Type == MessageTypes.SoftError)
          e.AddWarning(arioUrlValidationMessage.Text);
        
        return;
      }
      
      // Проверка логина и пароля.
      var loginResult = Functions.SmartProcessingSetting.Remote.Login(_obj, _obj.Password, true);
      if (!string.IsNullOrEmpty(loginResult.Error))
      {
        e.AddError(loginResult.Error);
        return;
      }
      
      // Проверка того, что заданные языки поддерживаются в Ario.
      var languagesValidationMessage = Functions.SmartProcessingSetting.ValidateLanguages(_obj);
      if (languagesValidationMessage != null)
      {
        e.AddError(languagesValidationMessage.Text);      
        return;
      }
      
      Dialogs.NotifyMessage(SmartProcessingSettings.Resources.ArioConnectionEstablished);
      
      // Проверка классификаторов.
      var classifiersValidationMessage = Functions.SmartProcessingSetting.ValidateClassifiers(_obj);
      if (classifiersValidationMessage != null)
        e.AddWarning(classifiersValidationMessage.Text);
    }
    
    public virtual bool CanCheckConnection(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate();
    }

  }

}