using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.StampSetting;
using Sungero.Docflow.Structures.StampSetting;

namespace Sungero.Docflow.Server
{
  partial class StampSettingFunctions
  {
    /// <summary>
    /// Получить дубли настройки отметки в документах.
    /// </summary>
    /// <returns>Дубли настройки отметки в документах.</returns>
    [Public, Remote(IsPure = true)]
    public virtual List<IStampSetting> GetStampSettingDuplicates()
    {
      var otherStampSettings = StampSettings.GetAll().Where(s => !Equals(s, _obj) && s.Status == Status.Active).ToList();
      var duplicates = new List<IStampSetting>();
      
      if (_obj.BusinessUnits.Any())
      {
        foreach (var businessUnit in _obj.BusinessUnits)
          duplicates.AddRange(otherStampSettings.Where(s => s.BusinessUnits.Any(b => b.BusinessUnit == businessUnit.BusinessUnit)).ToList());
      }
      else
        duplicates.AddRange(otherStampSettings.Where(s => !s.BusinessUnits.Any()).ToList());
      
      return duplicates;
    }
    
    /// <summary>
    /// Получить параметры простановки отметки для документа.
    /// </summary>
    /// <param name="signingDate">Дата и время подписания.</param>
    /// <param name="withCertificate">True - подпись с сертификатом, False - простая подпись.</param>
    /// <returns>Параметры простановки отметки.</returns>
    [Public]
    public virtual ISignatureStampParams GetSignatureStampParams(DateTime signingDate, bool withCertificate)
    {
      var stampParams = SignatureStampParams.Create();
      stampParams.Logo = this.GetLogoForSignatureStamp(withCertificate);
      stampParams.Title = this.GetTitleForSignatureStamp();
      stampParams.SigningDate = this.GetSigningDateForSignatureStamp(signingDate);
      
      return stampParams;
    }
    
    /// <summary>
    /// Получить подходящие настройки отметки об ЭП для документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Список подходящих настроек.</returns>
    [Public]
    public static List<IStampSetting> GetStampSettings(IOfficialDocument document)
    {
      var activeStampSettings = StampSettings.GetAll().Where(s => s.Status == Status.Active).ToList();
      if (document.BusinessUnit != null)
      {
        var stampSettingsForBusinessUnit = activeStampSettings.Where(s => s.BusinessUnits.Select(b => b.BusinessUnit).Contains(document.BusinessUnit)).ToList();
        if (stampSettingsForBusinessUnit.Any())
          return stampSettingsForBusinessUnit;
        else
          return activeStampSettings.Where(s => !s.BusinessUnits.Any()).ToList();
      }
      
      return activeStampSettings.Where(s => !s.BusinessUnits.Any()).ToList();
    }

    /// <summary>
    /// Получить логотип для простановки отметки.
    /// </summary>
    /// <param name="withCertificate">True - подпись с сертификатом, False - простая подпись.</param>
    /// <returns>Html-тег с логотипом.</returns>
    [Public]
    public virtual string GetLogoForSignatureStamp(bool withCertificate)
    {
      var image = string.Empty;
      if (_obj == null)
        image = Resources.SignatureStampSampleLogo;
      if (_obj.Logo == null)
        return string.Empty;

      image = _obj.LogoAsBase64;
      string logoImage = withCertificate ? Resources.HtmlStampLogoForCertificate : Resources.HtmlStampLogoForSignature;
      
      return logoImage.Replace("{Image}", image);
    }
    
    /// <summary>
    /// Получить дату и время сервера для простановки отметки.
    /// </summary>
    /// <param name="signingDate">Дата подписания.</param>
    /// <returns>Html-тег с датой и временем сервера или пустая строка.</returns>
    [Public]
    public virtual string GetSigningDateForSignatureStamp(DateTime signingDate)
    {
      var htmlSigningDate = string.Empty;
      var signingDateLabel = string.Empty;
      
      if (_obj != null && _obj.NeedShowDateTime == true)
      {
        using (Core.CultureInfoExtensions.SwitchTo(TenantInfo.Culture))
        {
          htmlSigningDate = Resources.HtmlStampSigningDateForSignature;
          
          var utcOffset = Calendar.UtcOffset.TotalHours;
          var utcOffsetLabel = utcOffset >= 0 ? "+" + utcOffset.ToString() : utcOffset.ToString();
          signingDateLabel = Docflow.PublicFunctions.Module.ToShortDateShortTime(signingDate.AddHours(utcOffset));
          signingDateLabel = string.Format("{0:g} (UTC{1})", signingDateLabel, utcOffsetLabel);
          htmlSigningDate = htmlSigningDate.Replace("{SigningDate}", signingDateLabel);
        }
      }
      
      return htmlSigningDate;
    }
    
    /// <summary>
    /// Получить заголовок для простановки отметки.
    /// </summary>
    /// <returns>Отформатированный заголовок.</returns>
    [Public]
    public virtual string GetTitleForSignatureStamp()
    {
      if (_obj == null)
        return Resources.SignatureStampSampleTitle;
      
      var stampSettingTitle = _obj.Title;
      // Убрать из строки html-теги.
      stampSettingTitle = System.Text.RegularExpressions.Regex.Replace(stampSettingTitle, @"<[^>]+>", string.Empty);
      // Заменить переносы строк на теги <br>.
      stampSettingTitle = stampSettingTitle.Replace("\r\n", "<br>").Replace("\n", "<br>");
      
      return stampSettingTitle;
    }

  }
}