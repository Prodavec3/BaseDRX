using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.SmartProcessingSetting;
using MessageTypes = Sungero.Docflow.Constants.SmartProcessingSetting.SettingsValidationMessageTypes;
using SettingsValidationMessageStructure = Sungero.Docflow.Structures.SmartProcessingSetting.SettingsValidationMessage;

namespace Sungero.Docflow.Server
{
  partial class SmartProcessingSettingFunctions
  {
    /// <summary>
    /// Создать настройки интеллектуальной обработки документов.
    /// </summary>
    [Remote, Public]
    public static void CreateSettings()
    {
      var smartProcessingSettings = SmartProcessingSettings.Create();
      
      // Заполнить из Docflow_Params, если ранее настройки хранились там.
      var arioUrlKey = Docflow.PublicFunctions.Module.GetDocflowParamsValue(Sungero.Docflow.PublicConstants.Module.ArioUrlKey);
      if (arioUrlKey != null)
        smartProcessingSettings.ArioUrl = arioUrlKey.ToString();
      
      var minFactProbability = Functions.Module.GetDocflowParamsNumbericValue(Sungero.Docflow.PublicConstants.Module.MinFactProbabilityKey);
      if (minFactProbability != 0)
        smartProcessingSettings.LowerConfidenceLimit = (int)minFactProbability;
      
      var trustedFactProbability = Functions.Module.GetDocflowParamsNumbericValue(Sungero.Docflow.PublicConstants.Module.TrustedFactProbabilityKey);
      if (trustedFactProbability != 0)
        smartProcessingSettings.UpperConfidenceLimit = (int)trustedFactProbability;
      
      smartProcessingSettings.Save();
      
      // Удалить параметры с настройками из Docflow_Params.
      Docflow.PublicFunctions.Module.ExecuteSQLCommand(Queries.SmartProcessingSetting.DeleteSmartSettingsFromDocflowParams);
    }
    
    /// <summary>
    /// Получить коннектор к Ario.
    /// </summary>
    /// <returns>Коннектор к Ario.</returns>
    public virtual Sungero.ArioExtensions.ArioConnector GetArioConnector()
    {
      var timeout = new TimeSpan(0, 0, GetArioConnectionTimeoutInSeconds());
      var password = string.IsNullOrEmpty(_obj.Password) ? string.Empty : Encryption.Decrypt(_obj.Password);
      return ArioExtensions.ArioConnector.Get(_obj.ArioUrl, timeout, _obj.Login, password);
    }
    
    /// <summary>
    /// Получить таймаут для подключения к Ario в секундах.
    /// </summary>
    /// <returns>Таймаут в секундах.</returns>
    [Public, Remote]
    public static int GetArioConnectionTimeoutInSeconds()
    {
      var timeout = (int)Functions.Module.GetDocflowParamsNumbericValue(PublicConstants.SmartProcessingSetting.ArioConnectionTimeoutKey);
      if (timeout == 0)
        timeout = PublicConstants.SmartProcessingSetting.ArioConnectionTimeoutInSeconds;
      
      return timeout;
    }
    
    /// <summary>
    /// Получить токен к Ario.
    /// </summary>
    /// <returns>Токен.</returns>
    [Public, Remote]
    public virtual string GetArioToken()
    {
      var password = string.IsNullOrEmpty(_obj.Password) ? string.Empty : Encryption.Decrypt(_obj.Password);
      return ArioExtensions.ArioConnector.GetArioToken(_obj.ArioUrl, _obj.Login, password);
    }
    
    /// <summary>
    /// Получить список классификаторов из Арио.
    /// </summary>
    /// <returns>Список классификаторов.</returns>
    [Remote(IsPure = true)]
    public virtual List<Structures.SmartProcessingSetting.ClassifierForDialog> GetArioClassifiers()
    {
      // TODO Rassokhina : убрать после починки передачи публичных структур с сервера на клиент Bug 71754, 67240.
      var classifiers = new List<Structures.SmartProcessingSetting.ClassifierForDialog>();
      foreach (var classifier in this.GetArioClassifiersPublic())
        classifiers.Add(Structures.SmartProcessingSetting.ClassifierForDialog.Create(classifier.Id, classifier.Name));
      return classifiers;
    }
    
    /// <summary>
    /// Проверить наличие классификаторов в Ario по точному соответствию наименования и ИД.
    /// </summary>
    /// <returns>True, если классификаторы существуют, иначе - False.</returns>
    [Remote(IsPure = true)]
    public virtual bool IsArioClassifiersExist()
    {
      var classifiers = this.GetArioClassifiersPublic();
      
      var firstPageClassifier = classifiers
        .Where(a => a.Id == _obj.FirstPageClassifierId.Value && a.Name == _obj.FirstPageClassifierName)
        .FirstOrDefault();
      var typeClassifier = classifiers
        .Where(a => a.Id == _obj.TypeClassifierId.Value && a.Name == _obj.TypeClassifierName)
        .FirstOrDefault();
      return firstPageClassifier != null && typeClassifier != null;
    }
    
    /// <summary>
    /// Получить список классификаторов из Арио.
    /// </summary>
    /// <returns>Список классификаторов.</returns>
    [Public]
    public virtual List<Docflow.Structures.SmartProcessingSetting.IClassifier> GetArioClassifiersPublic()
    {
      var classifiers = new List<Docflow.Structures.SmartProcessingSetting.IClassifier>();
      try
      {
        var arioConnector = this.GetArioConnector();
        classifiers = arioConnector.GetClassifiers()
          .Select(x => Docflow.Structures.SmartProcessingSetting.Classifier.Create(x.Id, x.Name)).ToList();
      }
      catch (Exception e)
      {
        Logger.Error("Error getting the list of classifiers from Ario.", e);
      }
      
      return classifiers;
    }
    
    /// <summary>
    /// Проверить, что языки поддерживаются в Ario.
    /// </summary>
    /// <returns>True, если поддерживаются, иначе - False.</returns>
    [Remote(IsPure = true)]
    public virtual bool CheckLanguagesSupportedByArio()
    {
      var supportedLanguages = this.GetArioSupportedLanguages();
      
      var inputLanguages = new List<string>(_obj.Languages.Trim().Split(';').Select(x => x.Trim()).ToList());
      inputLanguages  = inputLanguages.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
      var allLanguagesSupported = inputLanguages.All(x => supportedLanguages.Contains(x));
      return allLanguagesSupported;
    }
    
    /// <summary>
    /// Получить список поддерживаемых языков распознавания Ario.
    /// </summary>
    /// <returns>Список языков.</returns>
    [Remote(IsPure = true)]
    public virtual List<string> GetArioSupportedLanguages()
    {
      try
      {
        var arioConnector = this.GetArioConnector();
        return arioConnector.GetLanguages();
      }
      catch (Exception e)
      {
        Logger.Error("Error getting the supported languages list from Ario.", e);
        return new List<string>();
      }
    }
    
    /// <summary>
    /// Получить тело документа из Арио.
    /// </summary>
    /// <param name="documentGuid">Гуид документа в Арио.</param>
    /// <returns>Тело документа.</returns>
    [Public]
    public virtual System.IO.Stream GetDocumentBody(string documentGuid)
    {
      var arioConnector = this.GetArioConnector();
      return arioConnector.GetDocumentByGuid(documentGuid);
    }
    
    /// <summary>
    /// Установить основные параметры поступления документов.
    /// </summary>
    /// <param name="arioUrl">Адрес Арио.</param>
    /// <param name="lowerConfidenceLimit">Нижняя граница доверия извлеченным фактам.</param>
    /// <param name="upperConfidenceLimit">Верхняя граница доверия извлеченным фактам.</param>
    /// <param name="firstPageClassifierName">Имя классификатора первых страниц.</param>
    /// <param name="typeClassifierName">Имя классификатора по типам документов.</param>
    /// <returns>Ошибка, если заполнить настройки не удалось.</returns>
    public static Docflow.Structures.SmartProcessingSetting.SettingsValidationMessage SetSettings(string arioUrl,
                                                                                                  string lowerConfidenceLimit,
                                                                                                  string upperConfidenceLimit,
                                                                                                  string firstPageClassifierName,
                                                                                                  string typeClassifierName)
    {
      var smartProcessingSettings = Functions.SmartProcessingSetting.GetSettings();
      
      // Адрес.
      smartProcessingSettings.ArioUrl = arioUrl;
      var arioUrlValidationMessage = Functions.SmartProcessingSetting.ValidateArioUrl(smartProcessingSettings);
      if (arioUrlValidationMessage != null)
        return arioUrlValidationMessage;
      
      // Проверка корректности логина и пароля.
      var loginResult = Functions.SmartProcessingSetting.Login(smartProcessingSettings,
                                                               smartProcessingSettings.Password,
                                                               true);
      if (!string.IsNullOrEmpty(loginResult.Error))
        return SettingsValidationMessageStructure.Create(MessageTypes.Error, loginResult.Error);
      
      // Границы.
      int lowerLimit;
      int upperLimit;
      smartProcessingSettings.LowerConfidenceLimit = int.TryParse(lowerConfidenceLimit, out lowerLimit) ? lowerLimit : -1;
      smartProcessingSettings.UpperConfidenceLimit = int.TryParse(upperConfidenceLimit, out upperLimit) ? upperLimit : -1;
      var confidenceLimitsValidationMessage = Functions.SmartProcessingSetting.ValidateConfidenceLimits(smartProcessingSettings);
      if (confidenceLimitsValidationMessage != null)
        return confidenceLimitsValidationMessage;
      
      // Классификаторы.
      var classifiers = Functions.SmartProcessingSetting.GetArioClassifiers(smartProcessingSettings);
      var firstPageClassifier = classifiers.Where(a => a.Name == firstPageClassifierName).FirstOrDefault();
      var typeClassifier = classifiers.Where(a => a.Name == typeClassifierName).FirstOrDefault();
      
      if (firstPageClassifier == null || typeClassifier == null)
        return SettingsValidationMessageStructure.Create(MessageTypes.Error,
                                                         SmartProcessingSettings.Resources.SetCorrectClassifiers);
      
      smartProcessingSettings.FirstPageClassifierName = firstPageClassifier.Name;
      smartProcessingSettings.FirstPageClassifierId = firstPageClassifier.Id;
      smartProcessingSettings.TypeClassifierName = typeClassifier.Name;
      smartProcessingSettings.TypeClassifierId = typeClassifier.Id;
      
      smartProcessingSettings.Save();
      
      // Предупредить, что выбраны одинаковые классификаторы.
      if (firstPageClassifierName == typeClassifierName)
        return SettingsValidationMessageStructure.Create(MessageTypes.Warning,
                                                         SmartProcessingSettings.Resources.SetCorrectClassifiers);
      return null;
    }
    
    /// <summary>
    /// Проверить подключение к Ario.
    /// </summary>
    /// <returns>True, если сервис работает, иначе - False.</returns>
    /// <remarks>Проверка должна обязательно быть на сервере, т.к. с клиента может быть залочен доступ.</remarks>
    [Public, Remote]
    public virtual bool CheckConnection()
    {
      try
      {
        var arioConnector = ArioExtensions.ArioConnector.Get(_obj.ArioUrl);
        return arioConnector.CheckConnection();
      }
      catch (Exception e)
      {
        Logger.Error("Error connecting to Ario.", e);
      }
      return false;
    }
    
    /// <summary>
    /// Авторизация в сервисе Ario.
    /// </summary>
    /// <param name="password">Пароль.</param>
    /// <param name="passwordIsEncrypted">Пароль зашифрован.</param>
    /// <returns>Структура с зашифрованным паролем и текстом ошибки, если авторизация не успешна.</returns>
    [Remote]
    public Structures.SmartProcessingSetting.LoginResult Login(string password, bool passwordIsEncrypted)
    {
      var loginResult = new Structures.SmartProcessingSetting.LoginResult();
      
      // Дополнительно проверить, что требуется авторизация в сервисе Ario.
      // Чтобы вывести ошибку, если логин пустой, а авторизация требуется.
      var arioConnector = ArioExtensions.ArioConnector.Get(_obj.ArioUrl);
      if (string.IsNullOrEmpty(_obj.Login) && arioConnector.CheckAuthorizationEnabled())
      {
        loginResult.Error = Sungero.Docflow.SmartProcessingSettings.Resources.ArioLoginError;
        return loginResult;
      }
      
      // Проверить подключение по логину и паролю.
      try
      {
        if (passwordIsEncrypted && !string.IsNullOrEmpty(password))
          password = Encryption.Decrypt(password);
        ArioExtensions.ArioConnector.GetArioToken(_obj.ArioUrl, _obj.Login, password);
      }
      catch (Exception e)
      {
        if (string.Equals(e.Message, Constants.SmartProcessingSetting.ArioInvalidLoginPassword))
          loginResult.Error = Sungero.Docflow.SmartProcessingSettings.Resources.ArioLoginError;
        else
          loginResult.Error = Sungero.Docflow.SmartProcessingSettings.Resources.ArioConnectionError;
        return loginResult;
      }
      
      var encryptedPassword = Encryption.Encrypt(password ?? string.Empty);
      if (encryptedPassword != _obj.Password)
      {
        loginResult.EncryptedPassword = encryptedPassword;
      }
      
      return loginResult;
    }
  }
}