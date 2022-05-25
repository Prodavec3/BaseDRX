using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Bank;

namespace Sungero.Parties.Shared
{
  partial class BankFunctions
  {
    
    /// <summary>
    /// Проверить корректность SWIFT.
    /// </summary>
    /// <param name="swift">SWIFT.</param>
    /// <returns>Текст ошибки.
    /// Для верного SWIFT - пустая строка.</returns>
    [Public]
    public static string CheckSwift(string swift)
    {
      if (string.IsNullOrWhiteSpace(swift))
        return string.Empty;
      
      swift = swift.Trim();
      return System.Text.RegularExpressions.Regex.IsMatch(swift, @"(^[0-9A-Z]{8}$)|(^[0-9A-Z]{11}$)") ? string.Empty : Banks.Resources.IncorrectSwiftLength;
    }

    /// <summary>
    /// Проверка БИК по количеству символов.
    /// </summary>
    /// <param name="bic">БИК.</param>
    /// <returns>Пустая строка, если длина БИК в порядке.
    /// Иначе текст ошибки.</returns>
    [Public]
    public static string CheckBicLength(string bic)
    {
      if (string.IsNullOrWhiteSpace(bic))
        return string.Empty;
      
      bic = bic.Trim();
      return System.Text.RegularExpressions.Regex.IsMatch(bic, @"(^[0-9]{9}$)") ? string.Empty : Banks.Resources.IncorrectBicLength;
    }
    
    /// <summary>
    /// Проверка корр. счета по количеству символов.
    /// </summary>
    /// <param name="corr">Корр. счет.</param>
    /// <returns>Пустая строка, если длина корр. счета в порядке.
    /// Иначе текст ошибки.</returns>
    [Public]
    public static string CheckCorrLength(string corr)
    {
      if (string.IsNullOrWhiteSpace(corr))
        return string.Empty;
      
      corr = corr.Trim();
      return System.Text.RegularExpressions.Regex.IsMatch(corr, @"(^\d{20}$)") ? string.Empty : Banks.Resources.IncorrectCorrLength;
    }
    
    /// <summary>
    /// Проверка корр. счета для нерезидента.
    /// </summary>
    /// <param name="corr">Корр. счет.</param>
    /// <returns>Пустая строка, если корр. счет в порядке.
    /// Иначе текст ошибки.</returns>
    [Public]
    public static string CheckCorrAccountForNonresident(string corr)
    {
      if (string.IsNullOrWhiteSpace(corr))
        return string.Empty;
      
      corr = corr.Trim();
      return System.Text.RegularExpressions.Regex.IsMatch(corr, @"(^[0-9A-Z]*$)") ? string.Empty : Banks.Resources.IncorrectCorrNonresident;
    }

    /// <summary>
    /// Предупреждение о банке с аналогичным БИКом.
    /// </summary>
    /// <returns>Текст предупреждения с наименованием банка.</returns>
    [Public, Obsolete]
    public virtual string GetBankWithSameBicWarning()
    {
      var bankSameBic = Functions.Bank.Remote.GetBanksWithSameBic(_obj, true);
      if (bankSameBic.Any())
        return Banks.Resources.SameBICFormat(bankSameBic.First());
      return string.Empty;
    }
    
    /// <summary>
    /// Получить текст ошибки о наличии дублей контрагента.
    /// </summary>
    /// <returns>Текст ошибки.</returns>
    public override string GetCounterpartyDuplicatesErrorText()
    {
      var searchByBic = !string.IsNullOrWhiteSpace(_obj.BIC);
      var searchBySwift = !string.IsNullOrWhiteSpace(_obj.SWIFT);
      var foundByField = string.Empty;
      
      if (_obj.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed)
        return base.GetCounterpartyDuplicatesErrorText();
      
      var duplicates = new List<IBank>();
      if (searchByBic)
      {
        duplicates.AddRange(Functions.Bank.Remote.GetBanksWithSameBic(_obj, true));
        foundByField += _obj.Info.Properties.BIC.LocalizedName;
      }
      if (searchBySwift)
      {
        duplicates.AddRange(Functions.Bank.Remote.GetBanksWithSameSwift(_obj, true));
        foundByField += searchByBic ? "/" + _obj.Info.Properties.SWIFT.LocalizedName : _obj.Info.Properties.SWIFT.LocalizedName;
      }
      if (duplicates.Any())
      {
        var firstDuplicate = duplicates.OrderByDescending(x => x.Id).First();
        var duplicateTypeInNominative = GetTypeDisplayValue(firstDuplicate, CommonLibrary.DeclensionCase.Nominative);
        return Banks.Resources.SameBicAndOrSwiftFormat(foundByField, duplicateTypeInNominative.ToLower(), firstDuplicate);
      }
      
      return base.GetCounterpartyDuplicatesErrorText();
    }
    
    /// <summary>
    /// Получить банки, участвующие в договорах.
    /// </summary>
    /// <returns>Список ИД банков.</returns>
    [Public]
    public static List<int> GetBankIds()
    {
      return Functions.Bank.Remote.GetBankIdsServer();
    }
    
    /// <summary>
    /// Установить обязательность свойств в зависимости от заполненных данных.
    /// </summary>
    [Public]
    public void SetRequiredProperties()
    {
      var isNonresident = _obj.Nonresident == true;
      _obj.State.Properties.BIC.IsRequired = !isNonresident;
      _obj.State.Properties.SWIFT.IsRequired = isNonresident;
    }
  }
}