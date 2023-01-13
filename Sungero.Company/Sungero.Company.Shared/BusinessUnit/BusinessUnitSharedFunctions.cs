using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.BusinessUnit;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Shared
{
  partial class BusinessUnitFunctions
  {
    /// <summary>
    /// Получить текст ошибки о наличии дублей.
    /// </summary>
    /// <returns>Текст ошибки.</returns>
    public virtual string GetCounterpartyDuplicatesErrorText()
    {
      if (!string.IsNullOrWhiteSpace(_obj.TIN) && _obj.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
      {
        int? companyId = null;
        if (_obj.Company != null)
          companyId = _obj.Company.Id;
        
        var duplicateBusinessUnit = Functions.BusinessUnit.Remote.GetDuplicateBusinessUnit(_obj);
        if (duplicateBusinessUnit.Any())
        {
          var firstDuplicate = duplicateBusinessUnit.First();
          var duplicateTypeInNominative = Parties.PublicFunctions.Counterparty.GetTypeDisplayValue(firstDuplicate, CommonLibrary.DeclensionCase.Nominative);
          var errorText = string.IsNullOrWhiteSpace(_obj.TRRC)
            ? Parties.Counterparties.Resources.TinDuplicateFormat(duplicateTypeInNominative.ToLower(), firstDuplicate)
            : Parties.CompanyBases.Resources.TinTrrcDuplicateFormat(duplicateTypeInNominative.ToLower(), firstDuplicate);
          return errorText;
        }
        
        return Parties.PublicFunctions.Counterparty.GetCounterpartyWithSameTinWarning(_obj.TIN, _obj.TRRC, companyId);
      }
      
      return string.Empty;
    }
    
    /// <summary>
    /// Проверка введенного ИНН по количеству символов.
    /// </summary>
    /// <param name="tin">ИНН.</param>
    /// <returns>Пустая строка, если длина ИНН в порядке.
    /// Иначе текст ошибки.</returns>
    public virtual string CheckTinLength(string tin)
    {
      return System.Text.RegularExpressions.Regex.IsMatch(tin, @"(^\d{10}$)|(^\d{12}$)") ? string.Empty : Parties.CompanyBases.Resources.IncorrectTinLength;
    }
    
    /// <summary>
    /// Проверка введенного ОГРН по количеству символов.
    /// </summary>
    /// <param name="psrn">ОГРН.</param>
    /// <returns>Пустая строка, если длина ОГРН в порядке.
    /// Иначе текст ошибки.</returns>
    public virtual string CheckPsrnLength(string psrn)
    {
      if (string.IsNullOrWhiteSpace(psrn))
        return string.Empty;
      
      psrn = psrn.Trim();
      return System.Text.RegularExpressions.Regex.IsMatch(psrn, @"(^\d{13}$)|(^\d{15}$)") ? string.Empty : Parties.CompanyBases.Resources.IncorrecPsrnLength;
    }
    
    /// <summary>
    /// Проверка введенного ОКПО по количеству символов.
    /// </summary>
    /// <param name="nceo">ОКПО.</param>
    /// <returns>Пустая строка, если длина ОКПО в порядке.
    /// Иначе текст ошибки.</returns>
    public virtual string CheckNceoLength(string nceo)
    {
      if (string.IsNullOrWhiteSpace(nceo))
        return string.Empty;
      
      nceo = nceo.Trim();
      return System.Text.RegularExpressions.Regex.IsMatch(nceo, @"(^\d{8}$)|(^\d{10}$)|(^\d{14}$)") ? string.Empty : Parties.CompanyBases.Resources.IncorrecNceoLength;
    }
    
    /// <summary>
    /// Проверка контрольной суммы ИНН. Вызывается из CheckTinSum.
    /// </summary>
    /// <param name="tin">Строка ИНН. Передавать ИНН длиной 10-12 символов.</param>
    /// <param name="coefficients">Массив коэффициентов для умножения.</param>
    /// <returns>True, если контрольная сумма сошлась.</returns>
    private static bool CheckTinSum(string tin, int[] coefficients)
    {
      var sum = 0;
      for (var i = 0; i < coefficients.Count(); i++)
        sum += (int)char.GetNumericValue(tin[i]) * coefficients[i];
      sum = (sum % 11) % 10;
      return sum == (int)char.GetNumericValue(tin[coefficients.Count()]);
    }
    
    /// <summary>
    /// Проверка контрольной суммы ИНН.
    /// </summary>
    /// <param name="tin">ИНН.</param>
    /// <returns>True, если контрольная сумма сошлась.</returns>
    /// <remarks>Информация по ссылке: http://ru.wikipedia.org/wiki/Идентификационный_номер_налогоплательщика.</remarks>
    private bool CheckTinSum(string tin)
    {
      var coefficient10 = new int[] { 2, 4, 10, 3, 5, 9, 4, 6, 8 };
      var coefficient11 = new int[] { 7, 2, 4, 10, 3, 5, 9, 4, 6, 8 };
      var coefficient12 = new int[] { 3, 7, 2, 4, 10, 3, 5, 9, 4, 6, 8 };
      return tin.Length == 10 ? CheckTinSum(tin, coefficient10) : (CheckTinSum(tin, coefficient11) && CheckTinSum(tin, coefficient12));
    }
    
    /// <summary>
    /// Получить JSON-строку для индексирования в поисковой системе.
    /// </summary>
    /// <returns>JSON-строка.</returns>
    public virtual string GetIndexingJson()
    {
      return string.Format(Constants.BusinessUnit.ElasticsearchIndexTemplate,
                           _obj.Id,
                           Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(_obj.LegalName),
                           Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(_obj.Name),
                           _obj.TIN,
                           _obj.TRRC,
                           _obj.PSRN,
                           Sungero.Core.Calendar.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                           _obj.Status.Value.Value);
    }
  }
}