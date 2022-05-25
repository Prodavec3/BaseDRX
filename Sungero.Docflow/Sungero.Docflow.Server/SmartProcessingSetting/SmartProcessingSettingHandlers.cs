using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.SmartProcessingSetting;
using MessageTypes = Sungero.Docflow.Constants.SmartProcessingSetting.SettingsValidationMessageTypes;

namespace Sungero.Docflow
{
  partial class SmartProcessingSettingCaptureSourcesResponsiblePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CaptureSourcesResponsibleFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(x => Sungero.Company.Employees.Is(x) ||
                         Sungero.CoreEntities.Roles.Is(x) && Sungero.CoreEntities.Roles.As(x).IsSingleUser.Value);
    }
  }

  partial class SmartProcessingSettingServerHandlers
  {

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      throw AppliedCodeException.Create(Docflow.Resources.DeleteSettingsException);
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var isSavedFromUI = e.Params.Contains(Constants.SmartProcessingSetting.SaveFromUIParamName);
      if (!isSavedFromUI)
        return;
      
      // "Жёсткая" проверка адреса сервиса Ario.
      var arioUrlValidationMessage = Functions.SmartProcessingSetting.ValidateArioUrl(_obj);
      var isArioUrlValidationMessageError = arioUrlValidationMessage != null && arioUrlValidationMessage.Type == MessageTypes.Error;
      if (isArioUrlValidationMessageError)
        e.AddError(_obj.Info.Properties.ArioUrl, arioUrlValidationMessage.Text);
      
      // "Жёсткая" проверка корректности логина и пароля.
      var loginResult = Functions.SmartProcessingSetting.Login(_obj, _obj.Password, true);
      var isLoginValidationMessageError = !string.IsNullOrEmpty(loginResult.Error);
      if (isLoginValidationMessageError)
        e.AddError(loginResult.Error);
      
      // "Жёсткая" проверка того, что заданные языки поддерживаются в Ario.
      var isLanguagesValidationMessageError = false;
      if (arioUrlValidationMessage == null && !isLoginValidationMessageError)
      {
        var languagesValidationMessage = Functions.SmartProcessingSetting.ValidateLanguages(_obj);
        isLanguagesValidationMessageError = languagesValidationMessage != null;
        if (isLanguagesValidationMessageError)
          e.AddError(_obj.Info.Properties.Languages, languagesValidationMessage.Text);
      }
      
      // "Жёсткая" проверка границ доверия.
      var confidenceLimitsValidationMessage = Functions.SmartProcessingSetting.ValidateConfidenceLimits(_obj);
      var isConfidenceLimitsValidationMessageError = confidenceLimitsValidationMessage != null && confidenceLimitsValidationMessage.Type == MessageTypes.Error;
      if (isConfidenceLimitsValidationMessageError)
      {
        e.AddError(_obj.Info.Properties.LowerConfidenceLimit, confidenceLimitsValidationMessage.Text, _obj.Info.Properties.UpperConfidenceLimit);
        e.AddError(_obj.Info.Properties.UpperConfidenceLimit, confidenceLimitsValidationMessage.Text, _obj.Info.Properties.LowerConfidenceLimit);
      }
      
      // "Жёсткая" проверка источников поступления.
      var notUniqueNameSources = Functions.SmartProcessingSetting.GetNotUniqueNameSources(_obj);
      var isNotUniqueNameSourcesError = notUniqueNameSources != null;
      if (isNotUniqueNameSourcesError)
      {
        foreach (var source in notUniqueNameSources)
          e.AddError(source, source.Info.Properties.SenderLineName, SmartProcessingSettings.Resources.NotUniqueSenderLineNames);
      }
      
      // При наличии "Жёстких" ошибок не переходить к ForceSave.
      if (isArioUrlValidationMessageError || isLoginValidationMessageError ||
          isConfidenceLimitsValidationMessageError || isNotUniqueNameSourcesError ||
          isLanguagesValidationMessageError)
        return;
      
      var isForceSave = e.Params.Contains(Constants.SmartProcessingSetting.ForceSaveParamName);
      if (!isForceSave)
      {
        // "Мягкая" проверка адреса сервиса Ario.
        if (arioUrlValidationMessage != null && arioUrlValidationMessage.Type == MessageTypes.SoftError)
        {
          e.AddError(arioUrlValidationMessage.Text, _obj.Info.Actions.ForceSave);
          return;
        }
        
        // "Мягкая" проверка классификаторов.
        var classifierValidationMessage = Functions.SmartProcessingSetting.ValidateClassifiers(_obj);
        if (classifierValidationMessage != null)
          e.AddError(classifierValidationMessage.Text, _obj.Info.Actions.ForceSave);
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.LowerConfidenceLimit = 40;
      _obj.UpperConfidenceLimit = 80;
      
      _obj.Name = SmartProcessingSettings.Resources.SmartProcessingSettings;
      _obj.LimitsDescription = SmartProcessingSettings.Resources.LimitsDecriptionValue;
      _obj.Languages = Constants.SmartProcessingSetting.DefaultLanguages;
    }
  }

}