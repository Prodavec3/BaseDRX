using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.CompanyBase;

namespace Sungero.Parties.Shared
{
  partial class CompanyBaseFunctions
  {
    #region Проверка дублей
    
    /// <summary>
    /// Предупреждение о контрагенте с аналогичным ИНН или ИНН/КПП.
    /// </summary>
    /// <returns>Текст предупреждения с наименованием контрагента.</returns>
    [Public, Obsolete]
    public override string GetCounterpartyWithSameTinWarning()
    {
      if (_obj.Status != Sungero.CoreEntities.DatabookEntry.Status.Active)
        return string.Empty;
      
      return PublicFunctions.Counterparty.GetCounterpartyWithSameTinWarning(_obj.TIN, _obj.TRRC, _obj.Id);
    }
    
    /// <summary>
    /// Получить текст ошибки о наличии дублей контрагента.
    /// </summary>
    /// <returns>Текст ошибки.</returns>
    [Public]
    public override string GetCounterpartyDuplicatesErrorText()
    {
      // Не проверять для закрытых записей.
      if (_obj.Status != Sungero.CoreEntities.DatabookEntry.Status.Active)
        return string.Empty;
      
      var duplicates = this.GetDuplicates(true);
      var errorText = GenerateCounterpartyDuplicatesErrorText(duplicates, _obj.TRRC);
      return errorText;
    }
    
    /// <summary>
    /// Получить дубли организации.
    /// </summary>
    /// <param name="excludeClosed">Исключить закрытые записи.</param>
    /// <returns>Дубли организации.</returns>
    public override List<ICounterparty> GetDuplicates(bool excludeClosed)
    {
      // TODO Dmitriev_IA: На 53202, 69259 добавить поиск по имени.
      //                   По умолчанию для CompanyBase ищем по ИНН/КПП.
      return Functions.Module.Remote.GetDuplicateCounterparties(_obj.TIN, _obj.TRRC, string.Empty, _obj.Id, excludeClosed)
        .Where(x => CompanyBases.Is(x))
        .ToList();
    }
    
    #endregion
    
    /// <summary>
    /// Проверка, что контрагент - ИП.
    /// </summary>
    /// <returns>True, если контрагент - ИП, иначе False.</returns>
    public bool IsSelfEmployed()
    {
      if (!string.IsNullOrWhiteSpace(_obj.PSRN))
        return _obj.PSRN.Length == 15;
      
      if (!string.IsNullOrWhiteSpace(_obj.TIN))
        return _obj.TIN.Length == 12;
      
      if (!string.IsNullOrWhiteSpace(_obj.Name))
        return _obj.Name.StartsWith("ИП ");
      
      return false;
    }
    
    /// <summary>
    /// Проверка введенного ОГРН по количеству символов.
    /// </summary>
    /// <param name="psrn">ОГРН.</param>
    /// <returns>Пустая строка, если длина ОГРН в порядке.
    /// Иначе текст ошибки.</returns>
    public override string CheckPsrnLength(string psrn)
    {
      if (string.IsNullOrWhiteSpace(psrn))
        return string.Empty;
      
      // ОГРН должен состоять только из цифр.
      psrn = psrn.Trim();
      if (!Regex.IsMatch(psrn, @"^\d*$"))
        return CompanyBases.Resources.NotOnlyDigitsPsrn;
      
      return System.Text.RegularExpressions.Regex.IsMatch(psrn, @"(^\d{13}$)|(^\d{15}$)")
        ? string.Empty
        : CompanyBases.Resources.IncorrecPsrnLength;
    }
    
    /// <summary>
    /// Проверка введенного ОКПО по количеству символов.
    /// </summary>
    /// <param name="nceo">ОКПО.</param>
    /// <returns>Пустая строка, если длина ОКПО в порядке.
    /// Иначе текст ошибки.</returns>
    [Public]
    public override string CheckNceoLength(string nceo)
    {
      if (string.IsNullOrWhiteSpace(nceo))
        return string.Empty;
      
      nceo = nceo.Trim();
      return System.Text.RegularExpressions.Regex.IsMatch(nceo, @"(^\d{8}$)|(^\d{10}$)|(^\d{14}$)")
        ? string.Empty
        : CompanyBases.Resources.IncorrecNceoLength;
    }
    
    /// <summary>
    /// Проверка КПП на валидность.
    /// </summary>
    /// <param name="trrc">Строка с КПП.</param>
    /// <returns>Текст ошибки. Пустая строка для верного КПП.</returns>
    [Public]
    public static string CheckTRRC(string trrc)
    {
      if (string.IsNullOrWhiteSpace(trrc))
        return string.Empty;
      
      // КПП должен состоять только из цифр.
      trrc = trrc.Trim();
      if (!Regex.IsMatch(trrc, @"^\d*$"))
        return CompanyBases.Resources.NotOnlyDigitsTrrc;
      
      return System.Text.RegularExpressions.Regex.IsMatch(trrc, @"(^\d{9}$)")
        ? string.Empty
        : CompanyBases.Resources.IncorrectTrrcLength;
    }
    
    /// <summary>
    /// Получить JSON-строку для индексирования в поисковой системе.
    /// </summary>
    /// <returns>JSON-строка.</returns>
    [Public]
    public virtual string GetIndexingJson()
    {
      var emails = this.PrepareEmailForIndex(_obj.Email);
      var homepages = this.PrepareHomepageForIndex(_obj.Homepage);
      var phones = this.PreparePhonesForIndex(_obj.Phones);
      
      return string.Format(Constants.CompanyBase.ElasticsearchIndexTemplate,
                           _obj.Id,
                           Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(_obj.LegalName),
                           Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(_obj.Name),
                           _obj.HeadCompany != null ? Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(_obj.HeadCompany.DisplayValue) : string.Empty,
                           _obj.TIN,
                           _obj.TRRC,
                           _obj.PSRN,
                           homepages,
                           emails,
                           phones,
                           Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(_obj.LegalAddress),
                           Sungero.Core.Calendar.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                           _obj.Status.Value.Value);
    }
    
    /// <summary>
    /// Подготовить и отформатировать email для индексирования.
    /// </summary>
    /// <param name="email">Email.</param>
    /// <returns>Отформатированный email.</returns>
    public virtual string PrepareEmailForIndex(string email)
    {
      var result = string.Empty;
      if (!string.IsNullOrWhiteSpace(email))
      {
        var emails = new List<string>();
        var emailsMatch = Regex.Matches(email, @"^\S+@\S+.\S+");
        foreach (Match emailMatch in emailsMatch)
        {
          if (PublicFunctions.Module.EmailIsValid(emailMatch.Value))
            emails.Add(emailMatch.Value);
        }
        result = string.Join(" ", emails);
      }
      return result;
    }
    
    /// <summary>
    /// Подготовить и отформатировать сайт для индексирования.
    /// </summary>
    /// <param name="homepage">Сайт.</param>
    /// <returns>Отформатированный сайт.</returns>
    public virtual string PrepareHomepageForIndex(string homepage)
    {
      var result = string.Empty;
      if (!string.IsNullOrWhiteSpace(homepage))
      {
        var sites = new List<string>();
        foreach (var siteValue in homepage.Split(new char[] { ' ', ';', ',' }))
        {
          var site = Parties.PublicFunctions.Module.NormalizeSite(siteValue);
          if (!string.IsNullOrEmpty(site))
            sites.Add(site);
        }
        result = string.Join(" ", sites);
      }
      return result;
    }
    
    /// <summary>
    /// Подготовить и отформатировать телефоны для индексирования.
    /// </summary>
    /// <param name="phones">Телефоны.</param>
    /// <returns>Отформатированные телефоны.</returns>
    public virtual string PreparePhonesForIndex(string phones)
    {
      var result = string.Empty;
      if (!string.IsNullOrWhiteSpace(phones))
      {
        var phonesProcessed = new List<string>();
        var phonesMatch = Regex.Matches(phones, @"((8|\+7)[\- ]?)?(\(?\d{3,4}\)?[\- ]?)?[\d\- ]{5,10}");
        
        foreach (Match phoneMatch in phonesMatch)
        {
          var phone = Parties.PublicFunctions.Module.NormalizePhone(phoneMatch.Value);
          if (!string.IsNullOrEmpty(phone))
            phonesProcessed.Add(phone);
        }
        result = string.Join(" ", phonesProcessed);
      }
      return result;
    }

  }
}