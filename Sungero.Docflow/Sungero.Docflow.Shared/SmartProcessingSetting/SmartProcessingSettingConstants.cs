using System;
using Sungero.Core;

namespace Sungero.Docflow.Constants
{
  public static class SmartProcessingSetting
  {
    // Имя параметра "Сохранение выполняется через UI".
    public const string SaveFromUIParamName = "Settings Saved From UI";
    
    // Имя параметра "Сохранить принудительно".
    public const string ForceSaveParamName = "Settings Force Saved";
    
    // Типы ошибок при валидации настроек.
    public static class SettingsValidationMessageTypes
    {
      // Ошибка. При обработке документа в логах ошибка. Настройки сохранить нельзя.
      public const string Error = "Error";
      
      // "Мягкая" ошибка. При обработке документа в логах ошибка. Для сохранения требуется подтверждение.
      public const string SoftError = "SoftError";
      
      // Предупреждение. При обработке документа в логах предупреждение. Для сохранения требуется подтверждение.
      public const string Warning = "Warning";
    }
    
    // Сообщение при успешном подключении к Ario.
    public const string ArioConnectionSuccessMessage = "SmartService is running";
    
    // Сообщение, если не удалось подключиться к Ario по логину и паролю.
    public const string ArioInvalidLoginPassword = "invalid login or password";
    
    // Максимальная длина пароля.
    public const int PasswordMaxLength = 50;
    
    // Ключ параметра таймаута для подключения к Ario в секундах.
    [Sungero.Core.Public]
    public const string ArioConnectionTimeoutKey = "ArioConnectionTimeout";
    
    // Таймаут для подключения к Ario по умолчанию в секундах.
    [Sungero.Core.Public]
    public const int ArioConnectionTimeoutInSeconds = 600;
    
    // Языки распознавания по умолчанию.
    public const string DefaultLanguages = "rus; eng";
    
  }
}