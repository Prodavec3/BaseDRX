using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentRegister;

namespace Sungero.Docflow.Shared
{
  partial class DocumentRegisterFunctions
  {
    
    #region Журнал регистрации. Получение периода. Получение списка фильтрованных журналов
    
    /// <summary>
    /// Получить начало действия текущего периода журнала.
    /// </summary>
    /// <param name="registrationDate">Дата.</param>
    /// <returns>Начало периода, null для сквозной нумерации.</returns>
    [Public]
    public DateTime? GetBeginPeriod(DateTime registrationDate)
    {
      var year = this.GetCurrentYear(registrationDate);

      if (_obj.NumberingPeriod == NumberingPeriod.Year)
        return Calendar.GetDate(year, 1, 1);
      
      if (_obj.NumberingPeriod == NumberingPeriod.Quarter)
      {
        var quarter = this.GetCurrentQuarter(registrationDate);
        var firstMonth = ((quarter - 1) * 3) + 1;
        return Calendar.GetDate(year, firstMonth, 1);
      }
      
      if (_obj.NumberingPeriod == NumberingPeriod.Month)
      {
        var month = this.GetCurrentMonth(registrationDate);
        return Calendar.GetDate(year, month, 1);
      }
      
      if (_obj.NumberingPeriod == NumberingPeriod.Day)
      {
        return registrationDate.BeginningOfDay();
      }
      
      return null;
    }
    
    /// <summary>
    /// Получить конец действия текущего периода журнала.
    /// </summary>
    /// <param name="registrationDate">Дата.</param>
    /// <returns>Конец периода, null для сквозной нумерации.</returns>
    [Public]
    public DateTime? GetEndPeriod(DateTime registrationDate)
    {
      var periodBegin = this.GetBeginPeriod(registrationDate);
      if (_obj.NumberingPeriod == NumberingPeriod.Year)
        return periodBegin.Value.EndOfYear();
      if (_obj.NumberingPeriod == NumberingPeriod.Quarter)
        return periodBegin.Value.AddMonths(2).EndOfMonth();
      if (_obj.NumberingPeriod == NumberingPeriod.Month)
        return periodBegin.Value.EndOfMonth();
      if (_obj.NumberingPeriod == NumberingPeriod.Day)
        return periodBegin.Value.EndOfDay();
      
      return null;
    }

    /// <summary>
    /// Получить день периода действия текущего журнала.
    /// </summary>
    /// <param name="registrationDate">Текущая дата.</param>
    /// <returns>День периода для текущей даты.</returns>
    [Public]
    public int GetCurrentDay(DateTime registrationDate)
    {
      return _obj.NumberingPeriod == Docflow.DocumentRegister.NumberingPeriod.Day ? registrationDate.Day : 0;
    }
    
    /// <summary>
    /// Получить месяц периода действия текущего журнала.
    /// </summary>
    /// <param name="registrationDate">Текущая дата.</param>
    /// <returns>Месяц периода для текущей даты.</returns>
    [Public]
    public int GetCurrentMonth(DateTime registrationDate)
    {
      // Для разрезов не по месяцу вернуть ноль, для отличия их от разрезов по месяцу.
      // Вернуть null нельзя, т.к. параметр запроса будет считаться пустым.
      return _obj.NumberingPeriod == Docflow.DocumentRegister.NumberingPeriod.Day ||
        _obj.NumberingPeriod == Docflow.DocumentRegister.NumberingPeriod.Month ? registrationDate.Month : 0;
    }
    
    /// <summary>
    /// Получить квартал периода действия текущего журнала.
    /// </summary>
    /// <param name="registrationDate">Текущая дата.</param>
    /// <returns>Квартал периода для текущей даты.</returns>
    [Public]
    public int GetCurrentQuarter(DateTime registrationDate)
    {
      // Для разрезов не по кварталу вернуть 0, для отличия их от разрезов по кварталу.
      // Вернуть null нельзя, т.к. параметр запроса будет считаться пустым.
      if (_obj.NumberingPeriod != Docflow.DocumentRegister.NumberingPeriod.Quarter)
        return 0;
      
      if (registrationDate.Month <= 3)
        return 1;

      if (registrationDate.Month <= 6)
        return 2;

      if (registrationDate.Month <= 9)
        return 3;

      return 4;
    }

    /// <summary>
    /// Получить год периода действия текущего журнала.
    /// </summary>
    /// <param name="registrationDate">Текущая дата.</param>
    /// <returns>Год периода для текущей даты.</returns>
    [Public]
    public int GetCurrentYear(DateTime registrationDate)
    {
      return _obj.NumberingPeriod == Docflow.DocumentRegister.NumberingPeriod.Day ||
        _obj.NumberingPeriod == Docflow.DocumentRegister.NumberingPeriod.Month ||
        _obj.NumberingPeriod == Docflow.DocumentRegister.NumberingPeriod.Quarter ||
        _obj.NumberingPeriod == Docflow.DocumentRegister.NumberingPeriod.Year ? registrationDate.Year : 9999;
    }

    /// <summary>
    /// Получить квартал.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <returns>Квартал периода для текущей даты.</returns>
    [Public]
    public static string ToQuarterString(DateTime date)
    {
      if (date.Month <= 3)
        return "I";

      if (date.Month <= 6)
        return "II";

      if (date.Month <= 9)
        return "III";

      return "IV";
    }
    
    #endregion
    
    #region Работа с номером документа
    
    /// <summary>
    /// Заполнить пример номера журнала в соответствии с форматом.
    /// </summary>
    public virtual void FillValueExample()
    {
      _obj.ValueExample = this.GetValueExample();
    }
    
    /// <summary>
    /// Получить пример номера журнала в соответствии с форматом.
    /// </summary>
    /// <returns>Пример номера журнала.</returns>
    public virtual string GetValueExample()
    {
      var registrationIndexExample = "1";
      var leadingDocNumberExample = "1";
      var departmentCodeExample = DocumentRegisters.Resources.NumberFormatDepartmentCode;
      var caseFileIndexExample = DocumentRegisters.Resources.NumberFormatCaseFile;
      var businessUnitCodeExample = DocumentRegisters.Resources.NumberFormatBUCode;
      var docKindCodeExample = DocumentRegisters.Resources.NumberFormatDocKindCode;
      var counterpartyCodeExample = DocumentRegisters.Resources.NumberFormatCounterpartyCode;
      
      return Functions.DocumentRegister.GenerateRegistrationNumber(_obj, Calendar.UserNow, registrationIndexExample, leadingDocNumberExample,
                                                                   departmentCodeExample, businessUnitCodeExample, caseFileIndexExample, docKindCodeExample, counterpartyCodeExample, "0");
    }

    /// <summary>
    /// Генерировать регистрационный номер для документа.
    /// </summary>
    /// <param name="date">Дата регистрации.</param>
    /// <param name="index">Номер.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="leadingDocumentNumber">Номер ведущего документа.</param>
    /// <returns>Сгенерированный регистрационный номер.</returns>
    [Public]
    public virtual string GenerateRegistrationNumber(DateTime date, string index, string departmentCode, string businessUnitCode,
                                                     string caseFileIndex, string docKindCode, string counterpartyCode, string leadingDocumentNumber)
    {
      return Functions.DocumentRegister.GenerateRegistrationNumber(_obj, date, index, leadingDocumentNumber, departmentCode, businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, "0");
    }

    /// <summary>
    /// Генерировать регистрационный номер для диалога регистрации.
    /// </summary>
    /// <param name="date">Дата регистрации.</param>
    /// <param name="index">Номер.</param>
    /// <param name="leadingDocumentNumber">Номер ведущего документа.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="indexLeadingSymbol">Символ для заполнения ведущих значений индекса в номере.</param>
    /// <returns>Сгенерированный регистрационный номер.</returns>
    public virtual string GenerateRegistrationNumberFromDialog(DateTime date, string index, string leadingDocumentNumber,
                                                               string departmentCode, string businessUnitCode, string caseFileIndex,
                                                               string docKindCode, string counterpartyCode, string indexLeadingSymbol = "0")
    {
      return Functions.DocumentRegister.GenerateRegistrationNumber(_obj, date, index, leadingDocumentNumber, departmentCode, businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, indexLeadingSymbol);
    }
    
    /// <summary>
    /// Генерировать регистрационный номер для документа.
    /// </summary>
    /// <param name="date">Дата регистрации.</param>
    /// <param name="index">Номер.</param>
    /// <param name="leadingDocumentNumber">Номер ведущего документа.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="indexLeadingSymbol">Символ для заполнения ведущих значений индекса в номере.</param>
    /// <returns>Сгенерированный регистрационный номер.</returns>
    [Public]
    public virtual string GenerateRegistrationNumber(DateTime date, string index, string leadingDocumentNumber,
                                                     string departmentCode, string businessUnitCode, string caseFileIndex,
                                                     string docKindCode, string counterpartyCode, string indexLeadingSymbol)
    {
      // Сформировать регистрационный индекс.
      var registrationNumber = string.Empty;
      if (index.Length < _obj.NumberOfDigitsInNumber)
        registrationNumber = string.Concat(Enumerable.Repeat(indexLeadingSymbol, (_obj.NumberOfDigitsInNumber - index.Length) ?? 0));
      registrationNumber += index;
      
      // Сформировать регистрационный номер.
      var prefixAndPostfix = Functions.DocumentRegister.GenerateRegistrationNumberPrefixAndPostfix(_obj, date, leadingDocumentNumber, departmentCode,
                                                                                                   businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, false);
      return string.Format("{0}{1}{2}", prefixAndPostfix.Prefix, registrationNumber, prefixAndPostfix.Postfix);
    }
    
    /// <summary>
    /// Генерировать префикс и постфикс регистрационного номера документа.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <param name="leadingDocumentNumber">Ведущий документ.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="counterpartyCodeIsMetasymbol">Признак того, что код контрагента нужен в виде метасимвола.</param>
    /// <returns>Сгенерированный регистрационный номер.</returns>
    public virtual Structures.DocumentRegister.RegistrationNumberParts GenerateRegistrationNumberPrefixAndPostfix(DateTime date, string leadingDocumentNumber,
                                                                                                                  string departmentCode, string businessUnitCode,
                                                                                                                  string caseFileIndex, string docKindCode,
                                                                                                                  string counterpartyCode, bool counterpartyCodeIsMetasymbol)
    {
      var prefix = string.Empty;
      var postfix = string.Empty;
      var numberElement = string.Empty;
      var orderedNumberFormatItems = _obj.NumberFormatItems.OrderBy(f => f.Number);
      foreach (var element in orderedNumberFormatItems)
      {
        if (element.Element == Docflow.DocumentRegisterNumberFormatItems.Element.Number)
        {
          prefix = numberElement;
          numberElement = string.Empty;
        }
        else if (element.Element == Docflow.DocumentRegisterNumberFormatItems.Element.Log)
          numberElement += _obj.Index;
        else if (element.Element == Docflow.DocumentRegisterNumberFormatItems.Element.RegistrPlace && _obj.RegistrationGroup != null)
          numberElement += _obj.RegistrationGroup.Index;
        else if (element.Element == Docflow.DocumentRegisterNumberFormatItems.Element.Year2Place)
          numberElement += date.ToString("yy");
        else if (element.Element == Docflow.DocumentRegisterNumberFormatItems.Element.Year4Place)
          numberElement += date.ToString("yyyy");
        else if (element.Element == Docflow.DocumentRegisterNumberFormatItems.Element.Month)
          numberElement += date.ToString("MM");
        else if (element.Element == Docflow.DocumentRegisterNumberFormatItems.Element.Quarter)
          numberElement += ToQuarterString(date);
        else if (element.Element == Docflow.DocumentRegisterNumberFormatItems.Element.Day)
          numberElement += date.ToString("dd");
        else if (element.Element == Docflow.DocumentRegisterNumberFormatItems.Element.LeadingNumber)
          numberElement += leadingDocumentNumber;
        else if (element.Element == Docflow.DocumentRegisterNumberFormatItems.Element.DepartmentCode)
          numberElement += departmentCode;
        else if (element.Element == Docflow.DocumentRegisterNumberFormatItems.Element.BUCode)
          numberElement += businessUnitCode;
        else if (element.Element == Docflow.DocumentRegisterNumberFormatItems.Element.CaseFile)
          numberElement += caseFileIndex;
        else if (element.Element == Docflow.DocumentRegisterNumberFormatItems.Element.DocKindCode)
          numberElement += docKindCode;
        else if (element.Element == Docflow.DocumentRegisterNumberFormatItems.Element.CPartyCode && !counterpartyCodeIsMetasymbol)
          numberElement += counterpartyCode;
        else if (element.Element == Docflow.DocumentRegisterNumberFormatItems.Element.CPartyCode && counterpartyCodeIsMetasymbol)
          numberElement += DocumentRegisters.Resources.NumberFormatCounterpartyCode;
        
        // Не добавлять разделитель, для пустого кода контрагента.
        if (string.IsNullOrEmpty(counterpartyCode) || counterpartyCodeIsMetasymbol)
        {
          // Разделитель после пустого кода контрагента.
          if (element.Element == Docflow.DocumentRegisterNumberFormatItems.Element.CPartyCode)
            continue;
          
          // Разделитель до кода контрагента, если код контрагента последний в номере.
          var nextElement = orderedNumberFormatItems.Where(f => f.Number > element.Number).FirstOrDefault();
          var lastElement = orderedNumberFormatItems.LastOrDefault();
          if (nextElement != null && nextElement.Element == Docflow.DocumentRegisterNumberFormatItems.Element.CPartyCode &&
              lastElement != null && lastElement.Number == nextElement.Number)
            continue;
        }
        
        // Добавить разделитель.
        numberElement += element.Separator;
      }
      
      postfix = numberElement;
      return Structures.DocumentRegister.RegistrationNumberParts.Create(prefix, postfix);
    }
    
    /// <summary>
    /// Получить формат номера журнала регистрации для отчета.
    /// </summary>
    /// <returns>Формат номера для отчета.</returns>
    /// <remarks>Используется в SkippedNumbersReport.</remarks>
    public virtual string GetReportNumberFormat()
    {
      var numberFormat = string.Empty;

      foreach (var item in _obj.NumberFormatItems.OrderBy(x => x.Number))
      {
        var elementName = string.Empty;
        if (item.Element == DocumentRegisterNumberFormatItems.Element.Number)
          elementName = DocumentRegisters.Resources.NumberFormatNumber;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.Year2Place || item.Element == DocumentRegisterNumberFormatItems.Element.Year4Place)
          elementName = DocumentRegisters.Resources.NumberFormatYear;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.Quarter)
          elementName = DocumentRegisters.Resources.NumberFormatQuarter;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.Month)
          elementName = DocumentRegisters.Resources.NumberFormatMonth;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.Day)
          elementName = DocumentRegisters.Resources.NumberFormatDay;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.LeadingNumber)
          elementName = DocumentRegisters.Resources.NumberFormatLeadingNumber;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.Log)
          elementName = DocumentRegisters.Resources.NumberFormatLog;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.RegistrPlace)
          elementName = DocumentRegisters.Resources.NumberFormatRegistrPlace;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.CaseFile)
          elementName = DocumentRegisters.Resources.NumberFormatCaseFile;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.DepartmentCode)
          elementName = DocumentRegisters.Resources.NumberFormatDepartmentCode;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.BUCode)
          elementName = DocumentRegisters.Resources.NumberFormatBUCode;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.DocKindCode)
          elementName = DocumentRegisters.Resources.NumberFormatDocKindCode;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.CPartyCode)
          elementName = DocumentRegisters.Resources.NumberFormatCounterpartyCode;

        numberFormat += elementName + item.Separator;
      }

      return numberFormat;
    }
    
    /// <summary>
    /// Проверить возможность построения номера по разрезам журнала.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Сообщение об ошибке. Пустая строка, если возможно сформировать номер.</returns>
    public virtual string CheckDocumentRegisterSections(IOfficialDocument document)
    {
      var departmentValidationErrors = Functions.DocumentRegister.GetDepartmentValidationError(_obj, document);
      if (!Equals(departmentValidationErrors, string.Empty))
        return departmentValidationErrors;
      
      var businessUnitValidationErrors = Functions.DocumentRegister.GetBusinessUnitValidationError(_obj, document);
      if (!Equals(businessUnitValidationErrors, string.Empty))
        return businessUnitValidationErrors;
      
      var docKindCodeValidationErrors = Functions.DocumentRegister.GetDocumentKindValidationError(_obj, document);
      if (!Equals(docKindCodeValidationErrors, string.Empty))
        return docKindCodeValidationErrors;
      
      var docCaseFileValidationErrors = Functions.DocumentRegister.GetCaseFileValidationError(_obj, document);
      if (!Equals(docCaseFileValidationErrors, string.Empty))
        return docCaseFileValidationErrors;
      
      return string.Empty;
    }
    
    /// <summary>
    /// Проверить регистрационный номер на валидность.
    /// </summary>
    /// <param name="registrationDate">Дата регистрации.</param>
    /// <param name="registrationNumber">Номер регистрации.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="leadDocNumber">Номер ведущего документа.</param>
    /// <param name="searchCorrectingPostfix">Искать корректировочный постфикс.</param>
    /// <returns>Сообщение об ошибке. Пустая строка, если номер соответствует журналу.</returns>
    /// <remarks>Пример: 5/1-П/2020, где 5 - порядковый номер, П - индекс журнала, 2020 - год, /1 - корректировочный постфикс.</remarks>
    public virtual string CheckRegistrationNumberFormat(DateTime? registrationDate, string registrationNumber,
                                                        string departmentCode, string businessUnitCode, string caseFileIndex, string docKindCode, string counterpartyCode,
                                                        string leadDocNumber, bool searchCorrectingPostfix)
    {
      if (string.IsNullOrWhiteSpace(registrationNumber))
        return DocumentRegisters.Resources.EnterRegistrationNumber;
      
      // Регулярное выражение для рег. индекса.
      // "([0-9]+)" определяет, где искать индекс в номере.
      // "([\.\/-][0-9]+)?" определяет, где искать корректировочный постфикс в номере.
      // Пустые скобки в выражении @"([0-9]+)()" означают корректировочный постфикс,
      // чтобы количество групп в результате регулярного выражения было всегда одинаковым, независимо от того, нужно искать корректировочный постфикс или нет.
      var indexTemplate = searchCorrectingPostfix ? @"([0-9]+)([\.\/-][0-9]+)?" : @"([0-9]+)()";
      
      // Перед проверкой правильности формата дополнительно проверить наличие непечатных символов в строке ("\s").
      if (Regex.IsMatch(registrationNumber, @"\s"))
        return DocumentRegisters.Resources.NoSpaces;
      
      if (!GetRegexMatchFromRegistrationNumber(_obj, registrationDate ?? Calendar.UserToday, registrationNumber, indexTemplate,
                                               departmentCode, businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, leadDocNumber,
                                               string.Empty, string.Empty)
          .Success)
      {
        // Шаблон номера, состоящий из символов "*".
        var numberTemplate = string.Concat(Enumerable.Repeat("*", _obj.NumberOfDigitsInNumber.Value));
        var example = this.GenerateRegistrationNumber(registrationDate.Value, numberTemplate, departmentCode, businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, "0");
        return Docflow.Resources.RegistrationNumberNotMatchFormatFormat(example);
      }
      
      return string.Empty;
    }

    /// <summary>
    /// Получить сравнение рег.номера с шаблоном.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="date">Дата.</param>
    /// <param name="registrationNumber">Рег. номер.</param>
    /// <param name="indexTemplate">Шаблон номера.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="leadDocNumber">Номер ведущего документа.</param>
    /// <param name="numberPostfix">Постфикс номера.</param>
    /// <param name="additionalPrefix">Дополнительный префикс номера.</param>
    /// <returns>Индекс.</returns>
    internal static Match GetRegexMatchFromRegistrationNumber(IDocumentRegister documentRegister, DateTime date, string registrationNumber,
                                                              string indexTemplate, string departmentCode, string businessUnitCode,
                                                              string caseFileIndex, string docKindCode, string counterpartyCode, string leadDocNumber,
                                                              string numberPostfix, string additionalPrefix)
    {
      var prefixAndPostfix = Functions.DocumentRegister.GenerateRegistrationNumberPrefixAndPostfix(documentRegister, date, leadDocNumber, departmentCode,
                                                                                                   businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, true);
      var template = string.Format("{0}{1}{2}{3}", Regex.Escape(prefixAndPostfix.Prefix), 
                                                   indexTemplate, 
                                                   Regex.Escape(prefixAndPostfix.Postfix), 
                                                   numberPostfix);
      
      // Заменить метасимвол для кода контрагента на соответствующее регулярное выражение.
      var metaCounterpartyCode = Regex.Escape(DocumentRegisters.Resources.NumberFormatCounterpartyCode);
      template = template.Replace(metaCounterpartyCode, Constants.DocumentRegister.CounterpartyCodeRegex);
      
      // Совпадение в начале строки.
      var numberTemplate = string.Format("^{0}", template);
      var match = Regex.Match(registrationNumber, numberTemplate);
      if (match.Success)
        return match;
      
      // Совпадение в конце строки.
      numberTemplate = string.Format("{0}{1}$", additionalPrefix, template);
      return Regex.Match(registrationNumber, numberTemplate);
    }

    /// <summary>
    /// Получить индекс рег. номера.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="date">Дата.</param>
    /// <param name="registrationNumber">Рег. номер.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="leadDocNumber">Номер ведущего документа.</param>
    /// <param name="searchCorrectingPostfix">Искать корректировочный постфикс.</param>
    /// <returns>Индекс.</returns>
    /// <remarks>Пример: 5/1-П/2020, где 5 - порядковый номер, П - индекс журнала, 2020 - год, /1 - корректировочный постфикс.</remarks>
    public static int GetIndexFromRegistrationNumber(IDocumentRegister documentRegister, DateTime date, string registrationNumber,
                                                     string departmentCode, string businessUnitCode, string caseFileIndex, string docKindCode, string counterpartyCode,
                                                     string leadDocNumber, bool searchCorrectingPostfix)
    {
      return ParseRegistrationNumber(documentRegister, date, registrationNumber, departmentCode, businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, leadDocNumber, searchCorrectingPostfix).Index;
    }
    
    /// <summary>
    /// Выделить составные части рег.номера.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="date">Дата.</param>
    /// <param name="registrationNumber">Рег. номер.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="leadDocNumber">Номер ведущего документа.</param>
    /// <param name="searchCorrectingPostfix">Искать корректировочный постфикс.</param>
    /// <returns>Индекс рег.номера.</returns>
    /// <remarks>Пример: 5/1-П/2020, где 5 - порядковый номер, П - индекс журнала, 2020 - год, /1 - корректировочный постфикс.</remarks>
    public static Structures.DocumentRegister.RegistrationNumberIndex ParseRegistrationNumber(IDocumentRegister documentRegister, DateTime date, string registrationNumber,
                                                                                              string departmentCode, string businessUnitCode, string caseFileIndex, string docKindCode, string counterpartyCode,
                                                                                              string leadDocNumber, bool searchCorrectingPostfix)
    {
      // "(.*?)" определяет место, в котором находятся искомые данные.
      var releasingExpression = "(.*?)$";
      // Регулярное выражение для рег. индекса.
      // "([0-9]+)" определяет, где искать индекс в номере.
      // "([\.\/-][0-9]+)?" определяет, где искать корректировочный постфикс в номере.
      // Пустые скобки в выражении @"([0-9]+)()" означают корректировочный постфикс,
      // чтобы количество групп в результате регулярного выражения было всегда одинаковым, независимо от того, нужно искать корректировочный постфикс или нет.
      var indexExpression = searchCorrectingPostfix ? @"([0-9]+)([\.\/-][0-9]+)?" : @"([0-9]+)()";
      
      // Распарсить рег.номер на составляющие.
      var registrationNumberMatch = GetRegexMatchFromRegistrationNumber(documentRegister, date, registrationNumber, indexExpression,
                                                                        departmentCode, businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, leadDocNumber,
                                                                        releasingExpression, string.Empty);
      var indexOfNumber = registrationNumberMatch.Groups[1].Value;
      var correctingPostfix = registrationNumberMatch.Groups[2].Value;
      var postfix = registrationNumberMatch.Groups[3].Value;
      
      int index;
      index = int.TryParse(indexOfNumber, out index) ? index : 0;
      return Structures.DocumentRegister.RegistrationNumberIndex.Create(index, postfix, correctingPostfix);
    }
    
    /// <summary>
    /// Проверить совпадение рег.номеров.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="date">Дата.</param>
    /// <param name="registrationNumber">Рег. номер.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="leadDocNumber">Номер ведущего документа.</param>
    /// <param name="registrationNumberSample">Пример рег. номера.</param>
    /// <param name="searchCorrectingPostfix">Искать корректировочный постфикс.</param>
    /// <returns>True, если совпадают.</returns>
    /// <remarks>Пример: 5/1-П/2020, где 5 - порядковый номер, П - индекс журнала, 2020 - год, /1 - корректировочный постфикс.</remarks>
    public static bool IsEqualsRegistrationNumbers(IDocumentRegister documentRegister, DateTime date, string registrationNumber,
                                                   string departmentCode, string businessUnitCode, string caseFileIndex, string docKindCode, string counterpartyCode,
                                                   string leadDocNumber, string registrationNumberSample,
                                                   bool searchCorrectingPostfix)
    {
      var indexAndPostfix = ParseRegistrationNumber(documentRegister, date, registrationNumberSample, departmentCode, businessUnitCode,
                                                    caseFileIndex, docKindCode, counterpartyCode, leadDocNumber, searchCorrectingPostfix);
      var maxLeadZeroIndexCount = 9 - indexAndPostfix.Index.ToString().Count();
      var indexRegexTemplate = "[0]{0," + maxLeadZeroIndexCount + "}" + indexAndPostfix.Index + Regex.Escape(indexAndPostfix.CorrectingPostfix);
      
      return GetRegexMatchFromRegistrationNumber(documentRegister, date, registrationNumber, indexRegexTemplate, departmentCode,
                                                 businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, leadDocNumber,
                                                 indexAndPostfix.Postfix + "$", "^").Success;
    }
    
    #endregion
    
    #region Регистрация. Проверка составных частей формата номера
    
    /// <summary>
    /// Проверить заполненность подразделения и кода подразделения.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Текст ошибки, либо string.Empty.</returns>
    public static string GetDepartmentValidationError(IDocumentRegister documentRegister, IOfficialDocument document)
    {
      // Проверить необходимость кода подразделения.
      if (documentRegister == null || !documentRegister.NumberFormatItems.Any(n => n.Element == DocumentRegisterNumberFormatItems.Element.DepartmentCode))
        return string.Empty;
      
      // Проверить наличие подразделения.
      if (document.Department == null)
        return CreateValidationError(documentRegister, Docflow.Resources.FillDepartment);
      
      // Проверить наличие кода у подразделения.
      if (string.IsNullOrWhiteSpace(document.Department.Code))
        return CreateValidationError(documentRegister, Docflow.Resources.FillDepartmentCode);
      
      return string.Empty;
    }
    
    /// <summary>
    /// Проверить заполненность НОР и кода НОР.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Текст ошибки, либо string.Empty.</returns>
    public static string GetBusinessUnitValidationError(IDocumentRegister documentRegister, IOfficialDocument document)
    {
      // Проверить необходимость кода НОР.
      if (documentRegister == null || !documentRegister.NumberFormatItems.Any(n => n.Element == DocumentRegisterNumberFormatItems.Element.BUCode))
        return string.Empty;
      
      // Проверить наличие НОР.
      if (document.BusinessUnit == null)
        return CreateValidationError(documentRegister, Docflow.Resources.FillBusinessUnit);
      
      // Проверить наличие кода у НОР.
      if (string.IsNullOrWhiteSpace(document.BusinessUnit.Code))
        return CreateValidationError(documentRegister, Docflow.Resources.FillBusinessUnitCode);
      
      return string.Empty;
    }
    
    /// <summary>
    /// Проверить заполненность кода вида документа.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Текст ошибки, либо string.Empty.</returns>
    public static string GetDocumentKindValidationError(IDocumentRegister documentRegister, IOfficialDocument document)
    {
      // Проверить необходимость кода вида документа.
      if (documentRegister == null || !documentRegister.NumberFormatItems.Any(n => n.Element == DocumentRegisterNumberFormatItems.Element.DocKindCode))
        return string.Empty;
      
      // Проверить наличие вида документа.
      if (document.DocumentKind == null)
        return CreateValidationError(documentRegister, Docflow.Resources.FillDocumentKind);
      
      // Проверить наличие кода у вида документа.
      if (string.IsNullOrWhiteSpace(document.DocumentKind.Code))
        return CreateValidationError(documentRegister, Docflow.Resources.FillDocumentKindCode);
      
      return string.Empty;
    }
    
    /// <summary>
    /// Проверить заполненность дела.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Текст ошибки, либо string.Empty.</returns>
    public static string GetCaseFileValidationError(IDocumentRegister documentRegister, IOfficialDocument document)
    {
      // Проверить необходимость дела.
      if (documentRegister == null || !documentRegister.NumberFormatItems.Any(n => n.Element == DocumentRegisterNumberFormatItems.Element.CaseFile))
        return string.Empty;
      
      // Проверить наличие дела.
      if (document.CaseFile == null)
        return CreateValidationError(documentRegister, Docflow.Resources.FillCaseFile);
      
      // Проверить наличие кода у дела.
      if (string.IsNullOrEmpty(document.CaseFile.Index))
        return CreateValidationError(documentRegister, Docflow.Resources.FillCaseFileCode);
      
      return string.Empty;
    }
    
    /// <summary>
    /// Сформировать ошибку валидации.
    /// </summary>
    /// <param name="documentRegister">Журнал регистрации.</param>
    /// <param name="errorModel">Шаблон ошибки.</param>
    /// <returns>Ошибка валидации.</returns>
    public static string CreateValidationError(IDocumentRegister documentRegister, string errorModel)
    {
      if (documentRegister.RegisterType == Docflow.DocumentRegister.RegisterType.Numbering)
      {
        return string.Format(errorModel, Docflow.Resources.numberWord);
      }
      else
      {
        return string.Format(errorModel, Docflow.Resources.registerWord);
      }
    }
    
    #endregion
    
    /// <summary>
    /// Получить журнал по умолчанию для документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="filteredDocRegistersIds">Список ИД доступных журналов.</param>
    /// <param name="settingType">Тип настройки.</param>
    /// <returns>Журнал регистрации по умолчанию.</returns>
    /// <remarks>Журнал подбирается сначала из настройки регистрации, потом из персональных настроек пользователя.
    /// Если в настройках не указан журнал, или указан недействующий, то вернётся первый журнал из доступных для документа.
    /// Если доступных журналов несколько, то вернётся пустое значение.</remarks>
    public static IDocumentRegister GetDefaultDocRegister(IOfficialDocument document, List<int> filteredDocRegistersIds, Enumeration? settingType)
    {
      var defaultDocRegister = DocumentRegisters.Null;

      if (document == null)
        return defaultDocRegister;

      var registrationSetting = Docflow.PublicFunctions.RegistrationSetting.GetSettingByDocument(document, settingType);
      if (registrationSetting != null && filteredDocRegistersIds.Contains(registrationSetting.DocumentRegister.Id))
        return registrationSetting.DocumentRegister;
      
      var personalSettings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(null);
      if (personalSettings != null)
      {
        var documentKind = document.DocumentKind;

        if (documentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Incoming)
          defaultDocRegister = personalSettings.IncomingDocRegister;
        if (documentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Outgoing)
          defaultDocRegister = personalSettings.OutgoingDocRegister;
        if (documentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Inner)
          defaultDocRegister = personalSettings.InnerDocRegister;
        if (documentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Contracts)
          defaultDocRegister = personalSettings.ContractDocRegister;
      }

      if (defaultDocRegister == null || !filteredDocRegistersIds.Contains(defaultDocRegister.Id) || defaultDocRegister.Status != CoreEntities.DatabookEntry.Status.Active)
      {
        defaultDocRegister = null;
        if (filteredDocRegistersIds.Count() == 1)
          defaultDocRegister = Functions.DocumentRegister.Remote.GetDocumentRegister(filteredDocRegistersIds.First());
      }
      
      return defaultDocRegister;
    }

    /// <summary>
    /// Установить обязательность свойств в зависимости от заполненных данных.
    /// </summary>
    public virtual void SetRequiredProperties()
    {
      _obj.State.Properties.RegistrationGroup.IsRequired = _obj.Info.Properties.RegistrationGroup.IsRequired ||
        _obj.RegisterType == RegisterType.Registration;
      _obj.State.Properties.NumberFormatItems.IsRequired = true;
    }
    
    /// <summary>
    /// Отфильтровать документопотоки согласно настройкам выбранной группы регистрации.
    /// </summary>
    /// <param name="query">Все доступные документопотоки.</param>
    /// <returns>Отфильтрованные документопотоки.</returns>
    public List<Enumeration> GetFilteredDocumentFlows(IQueryable<Enumeration> query)
    {
      var group = _obj.RegistrationGroup;
      if (group != null)
      {
        if (group.CanRegisterIncoming != true)
          query = query.Where(f => f != DocumentFlow.Incoming);
        if (group.CanRegisterInternal != true)
          query = query.Where(f => f != DocumentFlow.Inner);
        if (group.CanRegisterOutgoing != true)
          query = query.Where(f => f != DocumentFlow.Outgoing);
        if (group.CanRegisterContractual != true)
          query = query.Where(f => f != DocumentFlow.Contracts);
      }
      return query.ToList();
    }
  }
}