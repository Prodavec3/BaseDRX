using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using Sungero.Parties.Counterparty;

namespace Sungero.Parties.Shared
{
  partial class CounterpartyFunctions
  {

    #region Проверка ИНН
    
    /// <summary>
    /// Проверка ИНН на валидность.
    /// </summary>
    /// <param name="tin">Строка с ИНН.</param>
    /// <param name="forCompany">Признак того, что проверяется ИНН для компании.</param>
    /// <returns>Текст ошибки. Пустая строка для верного ИНН.</returns>
    [Public]
    public static string CheckTin(string tin, bool forCompany)
    {
      if (string.IsNullOrWhiteSpace(tin))
        return string.Empty;
      
      tin = tin.Trim();
      
      // Проверить содержание ИНН. Должен состоять только из цифр. (Bug 87755)
      if (!Regex.IsMatch(tin, @"^\d*$"))
        return Sungero.Parties.Counterparties.Resources.NotOnlyDigitsTin;
      
      // Проверить длину ИНН. Для компаний допустимы ИНН длиной 10 или 12 символов, для персон - только 12.
      if (forCompany && tin.Length != 10 && tin.Length != 12)
        return CompanyBases.Resources.IncorrectTinLength;
      
      if (!forCompany && tin.Length != 12)
        return People.Resources.IncorrectTinLength;
      
      // Проверить значения первых 2х цифр на нули.
      // 1 и 2 цифры - код субъекта РФ (99 для межрегиональной ФНС для физлиц и ИП или код иностранной организации).
      if (tin.StartsWith("00"))
        return Counterparties.Resources.NotValidTinRegionCode;
      
      // Проверить контрольную сумму.
      if (!CheckTinSum(tin))
        return Counterparties.Resources.NotValidTin;
      
      return string.Empty;
    }

    /// <summary>
    /// Проверка ИНН на валидность.
    /// </summary>
    /// <param name="tin">Строка с ИНН.</param>
    /// <returns>Текст ошибки. Пустая строка для верного ИНН.</returns>
    public virtual string CheckTin(string tin)
    {
      return CheckTin(tin, true);
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
    private static bool CheckTinSum(string tin)
    {
      var coefficient10 = new int[] { 2, 4, 10, 3, 5, 9, 4, 6, 8 };
      var coefficient11 = new int[] { 7, 2, 4, 10, 3, 5, 9, 4, 6, 8 };
      var coefficient12 = new int[] { 3, 7, 2, 4, 10, 3, 5, 9, 4, 6, 8 };
      return tin.Length == 10 ? CheckTinSum(tin, coefficient10) : (CheckTinSum(tin, coefficient11) && CheckTinSum(tin, coefficient12));
    }
    
    #endregion

    #region Проверка дублей
    
    #region Устаревшие функции. Оставлены для совместимости
    
    /// <summary>
    /// Предупреждение о контрагенте с аналогичным ИНН.
    /// </summary>
    /// <returns>Текст предупреждения с наименованием контрагента.</returns>
    [Public, Obsolete]
    public virtual string GetCounterpartyWithSameTinWarning()
    {
      if (_obj.Status != Sungero.CoreEntities.DatabookEntry.Status.Active)
        return string.Empty;
      
      return GetCounterpartyWithSameTinWarning(_obj.TIN, null, _obj.Id);
    }
    
    #endregion
    
    /// <summary>
    /// Проверить наличие контрагентов с таким же ИНН.
    /// </summary>
    /// <param name="tin">ИНН.</param>
    /// <param name="trrc">КПП.</param>
    /// <param name="companyId">Ид текущей компании.</param>
    /// <returns>Текст предупреждения с наименованием контрагента, если контрагенты найдены.</returns>
    [Public]
    public static string GetCounterpartyWithSameTinWarning(string tin, string trrc, int? companyId)
    {
      var duplicates = GetDuplicateCounterparties(tin, trrc, string.Empty, companyId, true);
      var errorText = GenerateCounterpartyDuplicatesErrorText(duplicates, trrc);
      return errorText;
    }
    
    /// <summary>
    /// Получить текст ошибки о наличии дублей контрагента.
    /// </summary>
    /// <returns>Текст ошибки.</returns>
    [Public]
    public virtual string GetCounterpartyDuplicatesErrorText()
    {
      // Не проверять для закрытых записей.
      if (_obj.Status != Sungero.CoreEntities.DatabookEntry.Status.Active)
        return string.Empty;
      
      var duplicates = this.GetDuplicates(true);
      var errorText = GenerateCounterpartyDuplicatesErrorText(duplicates, string.Empty);
      return errorText;
    }
    
    /// <summary>
    /// Сформировать текст ошибки о наличии дублей контрагента.
    /// </summary>
    /// <param name="duplicates">Список дублей контрагента.</param>
    /// <param name="trrc">КПП контрагента.</param>
    /// <returns>Текст ошибки.</returns>
    public static string GenerateCounterpartyDuplicatesErrorText(List<ICounterparty> duplicates, string trrc)
    {
      if (duplicates.Any())
      {
        var firstDuplicate = duplicates.OrderByDescending(x => x.Id).First();
        var counterpartyTypeInNominative = GetTypeDisplayValue(firstDuplicate, CommonLibrary.DeclensionCase.Nominative);
        if (!string.IsNullOrWhiteSpace(trrc))
          return CompanyBases.Resources.TinTrrcDuplicateFormat(counterpartyTypeInNominative.ToLower(), firstDuplicate);
        
        return Counterparties.Resources.TinDuplicateFormat(counterpartyTypeInNominative.ToLower(), firstDuplicate);
      }
      
      return string.Empty;
    }
    
    /// <summary>
    /// Получить дубли контрагента.
    /// </summary>
    /// <param name="excludeClosed">Исключить закрытые записи.</param>
    /// <returns>Дубли контрагента.</returns>
    public virtual List<ICounterparty> GetDuplicates(bool excludeClosed)
    {
      // TODO Dmitriev_IA: На 53202, 69259 добавить поиск по имени.
      //                   По умолчанию для Counterparty ищем по ИНН.
      return Functions.Module.Remote.GetDuplicateCounterparties(_obj.TIN, string.Empty, string.Empty, _obj.Id, excludeClosed);
    }

    /// <summary>
    /// Получить список дублей контрагента.
    /// </summary>
    /// <param name="tin">ИНН.</param>
    /// <param name="trrc">КПП.</param>
    /// <param name="name">Наименование.</param>
    /// <param name="excludeClosed">Признак необходимости исключить закрытые записи.</param>
    /// <returns>Список дублей контрагента.</returns>
    [Public]
    public static List<ICounterparty> GetDuplicateCounterparties(string tin, string trrc, string name, bool excludeClosed = true)
    {
      return Functions.Module.Remote.GetDuplicateCounterparties(tin, trrc, name, excludeClosed);
    }
    
    /// <summary>
    /// Получить список дублей контрагента из входящего списка контрагентов.
    /// </summary>
    /// <param name="source">Список контрагентов, по которому идет поиск дублей.</param>
    /// <param name="tin">ИНН.</param>
    /// <param name="trrc">КПП.</param>
    /// <param name="name">Наименование контрагента.</param>
    /// <param name="excludeClosed">Признак необходимости исключить закрытые записи.</param>
    /// <returns>Список дублей контрагентов.</returns>
    [Public]
    public static List<ICounterparty> GetDuplicateCounterpartiesFromList(List<ICounterparty> source, string tin, string trrc, string name, bool excludeClosed = true)
    {
      return Functions.Module.Remote.GetDuplicateCounterpartiesFromList(source, tin, trrc, name, excludeClosed);
    }

    /// <summary>
    /// Получить дубли контрагента.
    /// </summary>
    /// <param name="tin">ИНН.</param>
    /// <param name="trrc">КПП.</param>
    /// <param name="name">Наименование.</param>
    /// <param name="excludedCounterpartyId">ИД контрагента, который будет исключен из списка дублей.</param>
    /// <param name="excludeClosed">Признак необходимости исключить закрытые записи.</param>
    /// <returns>Дубли контрагента.</returns>
    [Public]
    public static List<ICounterparty> GetDuplicateCounterparties(string tin, string trrc, string name, int? excludedCounterpartyId, bool excludeClosed = true)
    {
      return Functions.Module.Remote.GetDuplicateCounterparties(tin, trrc, name, excludedCounterpartyId, excludeClosed);
    }
    
    #endregion
    
    /// <summary>
    /// Проверка введенного ОГРН по количеству символов.
    /// </summary>
    /// <param name="psrn">ОГРН.</param>
    /// <returns>Пустая строка, если длина ОГРН в порядке.
    /// Иначе текст ошибки.</returns>
    public virtual string CheckPsrnLength(string psrn)
    {
      return string.Empty;
    }
    
    /// <summary>
    /// Проверка введенного ОКПО по количеству символов.
    /// </summary>
    /// <param name="nceo">ОКПО.</param>
    /// <returns>Пустая строка, если длина ОКПО в порядке.
    /// Иначе текст ошибки.</returns>
    [Public]
    public virtual string CheckNceoLength(string nceo)
    {
      return string.Empty;
    }
    
    /// <summary>
    /// Проверка номера счета по количеству символов.
    /// </summary>
    /// <param name="account">Номер счета.</param>
    /// <returns>Пустая строка, если длина номера счета в порядке.
    /// Иначе текст ошибки.</returns>
    [Public]
    public static string CheckAccountLength(string account)
    {
      if (string.IsNullOrWhiteSpace(account))
        return string.Empty;
      
      account = account.Trim();
      return System.Text.RegularExpressions.Regex.IsMatch(account, @"(^[0-9A-Z]{8,34}$)") ? string.Empty : Counterparties.Resources.IncorrectAccountLength;
    }
    
    /// <summary>
    /// Получить отображаемое имя типа сущности.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <param name="declension">Падеж.</param>
    /// <returns>Отображаемое имя типа сущности.</returns>
    [Public]
    public static string GetTypeDisplayValue(Sungero.Domain.Shared.IEntity entity, CommonLibrary.DeclensionCase declension = CommonLibrary.DeclensionCase.Nominative)
    {
      if (entity == null)
        return string.Empty;
      
      var entityFinalType = entity.GetType().GetFinalType();
      var entityTypeMetadata = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(entityFinalType);
      var displayName = entityTypeMetadata.GetDisplayName();
      
      return CommonLibrary.Padeg.ConvertCurrencyNameToTargetDeclension(displayName, declension);
    }
  }
}