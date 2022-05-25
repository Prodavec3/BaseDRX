using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentRegister;
using Sungero.Docflow.OfficialDocument;

namespace Sungero.Docflow
{
  partial class SkippedNumbersReportServerHandlers
  {
    
    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Docflow.PublicFunctions.Module.DeleteReportData(SkippedNumbersReport.SkipsTableName, SkippedNumbersReport.ReportSessionId);
      Docflow.PublicFunctions.Module.DeleteReportData(SkippedNumbersReport.AvailableDocumentsTableName, SkippedNumbersReport.ReportSessionId);
    }
    
    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var args = this.GetBeforeExecuteArguments();
      var documents = Enumerable.Empty<IOfficialDocument>().AsQueryable();
      AccessRights.AllowRead(() => { documents = this.FilterDocumentsByDocumentRegister(Docflow.OfficialDocuments.GetAll(), args); });
      
      documents = this.FilterDocumentsByNumberingSection(documents, args);
      documents = this.FilterDocumentsByDocumentRegisterPeriod(documents, args);
      var firstIndex = this.GetFirstDocumentIndexInPeriod(documents, args);
      var lastIndex = this.GetLastDocumentIndexInPeriod(documents, args);
      // Для случая когда нет документов в периоде.
      if (lastIndex < firstIndex)
        lastIndex = firstIndex - 1;
      documents = this.FilterDocumentsByIndicies(documents, firstIndex, lastIndex, args);
      
      args.HyperlinkMask = this.GetHyperlinkMask(documents);
      
      var skippedIndiciesIntervals = this.GetSkippedIndicesIntervals(firstIndex, lastIndex, args);
      var skippedNumberList = this.WriteToSkippedNumbersTable(skippedIndiciesIntervals, args);
      args.SkipedNumberList = this.GetSkippedNumberListDisplayValue(skippedNumberList);
      this.WriteToAvailableDocumentsTable(documents, args);
      
      this.UpdateReportProperties(args);
    }
    
    /// <summary>
    /// Заполнить свойства отчета из аргументов построения отчета.
    /// </summary>
    /// <param name="args">Аргументы построения отчета.</param>
    public virtual void UpdateReportProperties(Sungero.Docflow.Structures.SkippedNumbersReport.IBeforeExecuteArguments args)
    {
      SkippedNumbersReport.ReportSessionId = args.ReportSessionId;
      SkippedNumbersReport.CurrentDate = args.CurrentDate;
      SkippedNumbersReport.DocumentRegister = args.DocumentRegister;
      
      SkippedNumbersReport.LeadingDocument = args.LeadingDocument;
      SkippedNumbersReport.Department = args.Department;
      SkippedNumbersReport.BusinessUnit = args.BusinessUnit;
      
      SkippedNumbersReport.PeriodOffset = args.PeriodOffset;
      SkippedNumbersReport.Period = args.Period;
      SkippedNumbersReport.PeriodBegin = args.PeriodBegin;
      SkippedNumbersReport.PeriodEnd = args.PeriodEnd;
      
      SkippedNumbersReport.NumberFormat = args.NumberFormat;
      SkippedNumbersReport.hyperlinkMask = args.HyperlinkMask;
      SkippedNumbersReport.SkipedNumberList = args.SkipedNumberList;
    }
    
    /// <summary>
    /// Получить аргументы построения отчета.
    /// </summary>
    /// <returns>Аргументы построения отчета.</returns>
    /// <remarks>Используются свойства отчета, с которыми было вызвано его построение.</remarks>
    public virtual Sungero.Docflow.Structures.SkippedNumbersReport.IBeforeExecuteArguments GetBeforeExecuteArguments()
    {
      var args = Structures.SkippedNumbersReport.BeforeExecuteArguments.Create();
      args.ReportSessionId = Guid.NewGuid().ToString();
      args.CurrentDate = Calendar.Now;
      
      args.DocumentRegister = SkippedNumbersReport.DocumentRegister;
      if (SkippedNumbersReport.DocumentRegisterId.HasValue)
        args.DocumentRegister = DocumentRegisters.Get(SkippedNumbersReport.DocumentRegisterId.Value);
      
      args.LeadingDocument = SkippedNumbersReport.LeadingDocument;
      args.HasLeadingDocument = SkippedNumbersReport.LeadingDocument != null;
      args.Department = SkippedNumbersReport.Department;
      args.HasDepartment = SkippedNumbersReport.Department != null;
      args.BusinessUnit = SkippedNumbersReport.BusinessUnit;
      args.HasBusinessUnit = SkippedNumbersReport.BusinessUnit != null;
      
      /* См. Sungero.Docflow.Client.OfficialDocumentFunctions.RunRegistrationDialog(IOfficialDocument, DialogParams).
       * В стандартном решении SkippedNumbersReport.RegistrationDate заполняется только при вызове из диалога регистрации.
       */
      args.LaunchedFromDialog = SkippedNumbersReport.RegistrationDate.HasValue;
      args.RegistrationDate = SkippedNumbersReport.RegistrationDate;
      
      args.Period = this.GetPeriod(args);
      args.PeriodOffset = this.GetPeriodOffset(args);
      args.BaseDate = this.GetBaseDate(args);
      args.DocumentRegisterPeriodBegin = Functions.DocumentRegister.GetBeginPeriod(args.DocumentRegister, args.BaseDate);
      args.DocumentRegisterPeriodEnd = Functions.DocumentRegister.GetEndPeriod(args.DocumentRegister, args.BaseDate);
      args.PeriodBegin = this.GetPeriodBegin(args);
      args.PeriodEnd = this.GetPeriodEnd(args);
      args.NumberFormat = Functions.DocumentRegister.GetReportNumberFormat(args.DocumentRegister);
      
      return args;
    }
    
    /// <summary>
    /// Получить базовую дату для формирования отчета.
    /// </summary>
    /// <param name="args">Аргументы построения отчета.</param>
    /// <returns>Базовая дата для построения отчета.</returns>
    public DateTime GetBaseDate(Sungero.Docflow.Structures.SkippedNumbersReport.IBeforeExecuteArguments args)
    {
      var currentDate = args.CurrentDate;
      var periodOffset = args.PeriodOffset;
      
      if (args.LaunchedFromDialog)
        return args.RegistrationDate.Value;
      
      if (args.Period.Equals(Constants.SkippedNumbersReport.Year))
        return Calendar.EndOfYear(currentDate.AddYears(periodOffset));
      
      if (args.Period.Equals(Constants.SkippedNumbersReport.Quarter))
        return Calendar.EndOfDay(Docflow.PublicFunctions.AccountingDocumentBase.EndOfQuarter(currentDate.AddMonths(3 * periodOffset)));
      
      if (args.Period.Equals(Constants.SkippedNumbersReport.Month))
        return Calendar.EndOfMonth(currentDate.AddMonths(periodOffset));
      
      if (args.Period.Equals(Constants.SkippedNumbersReport.Week))
        return Calendar.EndOfWeek(currentDate.AddDays(7 * periodOffset));
      
      if (args.Period.Equals(Constants.SkippedNumbersReport.Day))
        return Calendar.EndOfDay(currentDate.AddDays(periodOffset));
      
      return currentDate;
    }
    
    /// <summary>
    /// Получить смещение периода отчета.
    /// </summary>
    /// <param name="args">Аргументы построения отчета.</param>
    /// <returns>Cмещение периода отчета.</returns>
    public int GetPeriodOffset(Sungero.Docflow.Structures.SkippedNumbersReport.IBeforeExecuteArguments args)
    {
      if (args.LaunchedFromDialog)
        return 0;
      return SkippedNumbersReport.PeriodOffset.HasValue ? SkippedNumbersReport.PeriodOffset.Value : 0;
    }
    
    /// <summary>
    /// Получить период отчета.
    /// </summary>
    /// <param name="args">Аргументы построения отчета.</param>
    /// <returns>Период отчета.</returns>
    public string GetPeriod(Sungero.Docflow.Structures.SkippedNumbersReport.IBeforeExecuteArguments args)
    {
      if (!args.LaunchedFromDialog)
        return SkippedNumbersReport.Period;
      
      if (args.DocumentRegister.NumberingPeriod == NumberingPeriod.Day)
        return Constants.SkippedNumbersReport.Day;
      
      return Constants.SkippedNumbersReport.Month;
    }
    
    /// <summary>
    /// Отфильтровать документы по журналу регистрации.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="args">Аргументы построения отчета.</param>
    /// <returns>Документы, отфильтрованные по журналу регистрации.</returns>
    /// <remarks>Документ должен быть зарегистрирован или зарезервирован в журнале.</remarks>
    public virtual IQueryable<IOfficialDocument> FilterDocumentsByDocumentRegister(IQueryable<IOfficialDocument> documents,
                                                                                   Sungero.Docflow.Structures.SkippedNumbersReport.IBeforeExecuteArguments args)
    {
      return documents
        .Where(d => d.DocumentRegister == args.DocumentRegister)
        .Where(d => d.RegistrationState == RegistrationState.Registered || d.RegistrationState == RegistrationState.Reserved);
    }
    
    /// <summary>
    /// Отфильтровать документы по разрезу журнала.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="args">Аргументы построения отчета.</param>
    /// <returns>Документы, отфильтрованные по разрезу журнала.</returns>
    public virtual IQueryable<IOfficialDocument> FilterDocumentsByNumberingSection(IQueryable<IOfficialDocument> documents,
                                                                                   Sungero.Docflow.Structures.SkippedNumbersReport.IBeforeExecuteArguments args)
    {
      if (args.HasLeadingDocument)
        documents = documents.Where(d => Equals(d.LeadingDocument, args.LeadingDocument));
      
      if (args.HasDepartment)
        documents = documents.Where(d => Equals(d.Department, args.Department));
      
      if (args.HasBusinessUnit)
        documents = documents.Where(d => Equals(d.BusinessUnit, args.BusinessUnit));
      
      return documents;
    }
    
    /// <summary>
    /// Отфильтровать документы по индексам.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="firstIndex">Индекс "с".</param>
    /// <param name="lastIndex">Индекс "по".</param>
    /// <param name="args">Аргументы построения отчета.</param>
    /// <returns>Документы, отфильтрованные по индекам.</returns>
    /// <remarks>Допускать документы с номером, не соответствующим формату (Index = 0).</remarks>
    public virtual IQueryable<IOfficialDocument> FilterDocumentsByIndicies(IQueryable<IOfficialDocument> documents,
                                                                           int firstIndex,
                                                                           int lastIndex,
                                                                           Sungero.Docflow.Structures.SkippedNumbersReport.IBeforeExecuteArguments args)
    {
      return documents.Where(l => l.Index >= firstIndex && l.Index <= lastIndex ||
                                  l.Index == 0 && l.RegistrationDate <= args.PeriodEnd && l.RegistrationDate >= args.PeriodBegin);
    }
    
    /// <summary>
    /// Отфильтровать документы по периоду журнала регистрации.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="args">Аргументы построения отчета.</param>
    /// <returns>Документы, отфильтрованые по периоду журнала регистрации.</returns>
    public virtual IQueryable<IOfficialDocument> FilterDocumentsByDocumentRegisterPeriod(IQueryable<IOfficialDocument> documents,
                                                                                         Sungero.Docflow.Structures.SkippedNumbersReport.IBeforeExecuteArguments args)
    {
      DateTime? documentRegisterPeriodBegin = args.DocumentRegisterPeriodBegin;
      DateTime? documentRegisterPeriodEnd = args.DocumentRegisterPeriodEnd;
      
      return documents
        .Where(d => !documentRegisterPeriodBegin.HasValue || d.RegistrationDate >= documentRegisterPeriodBegin)
        .Where(d => !documentRegisterPeriodEnd.HasValue || d.RegistrationDate <= documentRegisterPeriodEnd);
    }
    
    /// <summary>
    /// Получить дату начала периода отчета.
    /// </summary>
    /// <param name="args">Аргументы построения отчета.</param>
    /// <returns>Дата начала периода отчета.</returns>
    /// <remarks>При построении отчета из диалога регистрации берем данные за последний календарный месяц от текущего дня, если период журнала месяц и более.</remarks>
    /// <remarks>При построении отчета из диалога регистрации берем данные за последний день, если период журнала равен дню.</remarks>
    public virtual DateTime GetPeriodBegin(Sungero.Docflow.Structures.SkippedNumbersReport.IBeforeExecuteArguments args)
    {
      var baseDate = args.BaseDate;
      if (args.LaunchedFromDialog)
      {
        if (args.DocumentRegister.NumberingPeriod == NumberingPeriod.Day)
          return Calendar.BeginningOfDay(baseDate);
        return baseDate.AddMonths(-1);
      }
      
      var periodBegin = baseDate;
      
      if (args.Period.Equals(Constants.SkippedNumbersReport.Year))
        periodBegin = Calendar.BeginningOfYear(baseDate);
      
      if (args.Period.Equals(Constants.SkippedNumbersReport.Quarter))
        periodBegin = Docflow.PublicFunctions.AccountingDocumentBase.BeginningOfQuarter(baseDate);
      
      if (args.Period.Equals(Constants.SkippedNumbersReport.Month))
        periodBegin = Calendar.BeginningOfMonth(baseDate);
      
      if (args.Period.Equals(Constants.SkippedNumbersReport.Week))
        periodBegin = Calendar.BeginningOfWeek(baseDate);
      
      if (args.Period.Equals(Constants.SkippedNumbersReport.Day))
        periodBegin = Calendar.BeginningOfDay(baseDate);
      
      if (args.DocumentRegisterPeriodBegin.HasValue && args.DocumentRegisterPeriodBegin.Value > periodBegin)
        return args.DocumentRegisterPeriodBegin.Value;
      
      return periodBegin;
    }
    
    /// <summary>
    /// Получить дату конца периода отчета.
    /// </summary>
    /// <param name="args">Аргументы построения отчета.</param>
    /// <returns>Дата конца периода отчета.</returns>
    /// <remarks>При построении отчета из диалога регистрации берем данные за последний календарный месяц по текущий день включительно.</remarks>
    public virtual DateTime GetPeriodEnd(Sungero.Docflow.Structures.SkippedNumbersReport.IBeforeExecuteArguments args)
    {
      var baseDate = args.BaseDate;
      
      if (args.LaunchedFromDialog)
        return Calendar.EndOfDay(baseDate);
      
      var periodEnd = Calendar.SqlMaxValue;
      if (args.Period.Equals(Constants.SkippedNumbersReport.Year))
        periodEnd = Calendar.EndOfYear(baseDate);

      if (args.Period.Equals(Constants.SkippedNumbersReport.Quarter))
        periodEnd = Calendar.EndOfDay(Docflow.PublicFunctions.AccountingDocumentBase.EndOfQuarter(baseDate));
      
      if (args.Period.Equals(Constants.SkippedNumbersReport.Month))
        periodEnd = Calendar.EndOfMonth(baseDate);
      
      if (args.Period.Equals(Constants.SkippedNumbersReport.Week))
        periodEnd = Calendar.EndOfWeek(baseDate);
      
      if (args.Period.Equals(Constants.SkippedNumbersReport.Day))
        periodEnd = Calendar.EndOfDay(baseDate);
      
      if (args.DocumentRegisterPeriodEnd.HasValue && args.DocumentRegisterPeriodEnd.Value < periodEnd)
        return args.DocumentRegisterPeriodEnd.Value;
      
      return periodEnd;
    }
    
    /// <summary>
    /// Получить первый индекс по периоду отчета.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="args">Аргументы построения отчета.</param>
    /// <returns>Первый индекс по периоду отчета.</returns>
    public virtual int GetFirstDocumentIndexInPeriod(IQueryable<IOfficialDocument> documents,
                                                     Sungero.Docflow.Structures.SkippedNumbersReport.IBeforeExecuteArguments args)
    {
      var periodBegin = args.PeriodBegin;
      var periodEnd = args.PeriodEnd;
      
      // Получить минимальный индекс по документам в периоде (при ручной регистрации мб нарушение следования индексов).
      var firstDocumentIndex = Functions.DocumentRegister.GetIndex(documents, periodBegin, periodEnd, false);
      
      // Получить индекс документа из предыдущего периода.
      var previousIndex = 0;
      if (periodBegin != args.DocumentRegisterPeriodBegin)
        previousIndex = Functions.DocumentRegister.FilterDocumentsByPeriod(documents, args.DocumentRegisterPeriodBegin,
                                                                           periodBegin.AddDays(-1).EndOfDay())
          .Where(d => !firstDocumentIndex.HasValue || d.Index < firstDocumentIndex).Select(d => d.Index).OrderByDescending(a => a).FirstOrDefault() ?? 0;
      
      if (!firstDocumentIndex.HasValue)
        firstDocumentIndex = previousIndex + 1;
      
      return firstDocumentIndex < previousIndex ? firstDocumentIndex.Value : previousIndex + 1;
    }
    
    /// <summary>
    /// Получить последний индекс по периоду отчета.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="args">Аргументы построения отчета.</param>
    /// <returns>Последний индекс по периоду отчета.</returns>
    public virtual int GetLastDocumentIndexInPeriod(IQueryable<IOfficialDocument> documents,
                                                    Sungero.Docflow.Structures.SkippedNumbersReport.IBeforeExecuteArguments args)
    {
      var periodBegin = args.PeriodBegin;
      var periodEnd = args.PeriodEnd;
      
      // Получить первый индекс документа следующего периода.
      var nextIndex = periodEnd != args.DocumentRegisterPeriodEnd ?
        Functions.DocumentRegister.GetIndex(documents, periodEnd.AddDays(1).BeginningOfDay(), args.DocumentRegisterPeriodEnd, false) : null;
      
      var leadingDocumentId = args.HasLeadingDocument ? args.LeadingDocument.Id : 0;
      var departmentId = args.HasDepartment ? args.Department.Id : 0;
      var businessUnitId = args.HasBusinessUnit ? args.BusinessUnit.Id : 0;
      
      // Если в следующем периоде ещё нет документов, то взять текущий индекс журнала.
      if (!nextIndex.HasValue)
        nextIndex = Functions.DocumentRegister.GetCurrentNumber(args.DocumentRegister, args.BaseDate, leadingDocumentId, departmentId, businessUnitId) + 1;
      
      // Получить индекс по зарегистрированным документам (при ручной регистрации мб нарушение следования индексов).
      var lastDocumentIndex = Functions.DocumentRegister.GetIndex(documents, periodBegin, periodEnd, true) ?? nextIndex.Value - 1;
      return lastDocumentIndex >= nextIndex ? lastDocumentIndex : nextIndex.Value - 1;
    }
    
    /// <summary>
    /// Получить маску для гиперссылок на документы.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <returns>Маска для гиперссылок на документы.</returns>
    public virtual string GetHyperlinkMask(IQueryable<IOfficialDocument> documents)
    {
      if (!documents.Any())
        return string.Empty;
      
      var link = Hyperlinks.Get(documents.First());
      var index = link.IndexOf("?type=");
      return link.Substring(0, index) + "?type=DocGUID&id=DocId";
    }
    
    /// <summary>
    /// Получить интервалы пропущенных индексов журнала.
    /// </summary>
    /// <param name="firstIndex">Индекс "с".</param>
    /// <param name="lastIndex">Индекс "по".</param>
    /// <param name="args">Аргументы построения отчета.</param>
    /// <returns>Интервалы пропущенных индексов журнала.</returns>
    public virtual Dictionary<int, int> GetSkippedIndicesIntervals(int firstIndex,
                                                                   int lastIndex,
                                                                   Sungero.Docflow.Structures.SkippedNumbersReport.IBeforeExecuteArguments args)
    {
      var periodBeginDate = args.DocumentRegisterPeriodBegin.HasValue ? args.DocumentRegisterPeriodBegin.Value : Calendar.SqlMinValue;
      var month = periodBeginDate.ToString("MM");
      var day = periodBeginDate.ToString("dd");
      var startDate = string.Format("{0}{1}{2}", periodBeginDate.Year, month, day);
      
      var periodEndDate = args.DocumentRegisterPeriodEnd.HasValue ? args.DocumentRegisterPeriodEnd.Value : args.PeriodEnd;
      month = periodEndDate.ToString("MM");
      day = periodEndDate.ToString("dd");
      var endDate = string.Format("{0}{1}{2}", periodEndDate.Year, month, day);
      
      var queryText = string.Format(Queries.SkippedNumbersReport.GetSkippedIndexes,
                                    args.DocumentRegister.Id.ToString(),
                                    (firstIndex - 1).ToString(),
                                    (lastIndex + 1).ToString(),
                                    args.HasBusinessUnit.ToString(),
                                    args.HasBusinessUnit ? args.BusinessUnit.Id.ToString() : "0",
                                    args.HasDepartment.ToString(),
                                    args.HasDepartment ? args.Department.Id.ToString() : "0",
                                    args.HasLeadingDocument.ToString(),
                                    args.HasLeadingDocument ? args.LeadingDocument.Id.ToString() : "0",
                                    args.DocumentRegisterPeriodBegin.HasValue.ToString(),
                                    startDate,
                                    endDate);
      
      // Получить интервалы пропущенных индексов журнала в периоде.
      // Key - начало интервала, Value - окончание интервала.
      var skippedIndiciesIntervals = new Dictionary<int, int>();
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = queryText;
        var result = command.ExecuteReader();
        while (result.Read())
        {
          skippedIndiciesIntervals.Add((int)result[1], (int)result[0]);
        }
        result.Close();
      }
      
      return skippedIndiciesIntervals;
    }
    
    /// <summary>
    /// Заполнить отчет данными пропущенных индексов.
    /// </summary>
    /// <param name="skippedIndexIntervals">Пропущенные индексы.</param>
    /// <param name="args">Аргументы построения отчета.</param>
    /// <returns>Записи, внесенные в таблицу, в виде интервалов или отдельных пропущенных индексов.</returns>
    public virtual List<string> WriteToSkippedNumbersTable(IDictionary<int, int> skippedIndexIntervals,
                                                           Sungero.Docflow.Structures.SkippedNumbersReport.IBeforeExecuteArguments args)
    {
      var skippedNumberList = new List<string>();
      var skippedNumbers = new List<Structures.SkippedNumbersReport.SkippedNumber>();
      var reportSessionId = args.ReportSessionId;
      
      foreach (var interval in skippedIndexIntervals)
      {
        var intervalStart = interval.Key;
        var intervalEnd = interval.Value;

        // Три и более подряд идущих пропущенных индексов должны быть собраны в одну строку.
        var intervalLength = intervalEnd - intervalStart + 1;
        if (intervalLength >= 3)
        {
          skippedNumbers.Add(Structures.SkippedNumbersReport.SkippedNumber.Create(Docflow.Reports.Resources.SkippedNumbersReport.NumbersAreSkipped,
                                                                                  string.Format("{0}-{1}", intervalStart.ToString(), intervalEnd.ToString()),
                                                                                  intervalStart,
                                                                                  reportSessionId));
          skippedNumberList.Add(string.Format("{0}-{1}",
                                              intervalStart.ToString(),
                                              intervalEnd.ToString()));
          
          continue;
        }
        
        for (var i = intervalStart; i <= intervalEnd; i++)
        {
          skippedNumbers.Add(Structures.SkippedNumbersReport.SkippedNumber.Create(Docflow.Reports.Resources.SkippedNumbersReport.NumberIsSkipped,
                                                                                  i.ToString(),
                                                                                  i,
                                                                                  reportSessionId));
          skippedNumberList.Add(i.ToString());
        }
      }
      
      var skipsTableName = Constants.SkippedNumbersReport.SkipsTableName;
      SkippedNumbersReport.SkipsTableName = skipsTableName;
      Functions.Module.WriteStructuresToTable(skipsTableName, skippedNumbers);
      
      return skippedNumberList;
    }
    
    /// <summary>
    /// Получить отображаемое значение для списка пропущенных индексов.
    /// </summary>
    /// <param name="skippedNumberList">Список пропущенных индексов.</param>
    /// <returns>Отображаемое значение для списка пропущенных индексов.</returns>
    /// <remarks>Получить 8-10 первых пропущенных номеров строкой. Для остальных указать общее количество.</remarks>
    public virtual string GetSkippedNumberListDisplayValue(List<string> skippedNumberList)
    {
      var displayValue = string.Empty;
      var skippedNumberCount = skippedNumberList.Count;
      var maxDisplayedNumberCount = 10;
      var minHiddenNumberCount = 3;
      var displayedValuesCount = skippedNumberCount;
      if (skippedNumberCount >= (maxDisplayedNumberCount + minHiddenNumberCount))
        displayedValuesCount = maxDisplayedNumberCount;
      else if (skippedNumberCount > maxDisplayedNumberCount)
        displayedValuesCount = skippedNumberCount - minHiddenNumberCount;
      
      displayValue = string.Join(", ", skippedNumberList.ToArray(), 0, displayedValuesCount);
      var hiddenSkippedNumberCount = skippedNumberCount - displayedValuesCount;
      if (hiddenSkippedNumberCount > 0)
      {
        var numberLabel = Functions.Module.GetNumberDeclination(hiddenSkippedNumberCount,
                                                                Resources.SkippedNumbersReportNumber,
                                                                Resources.SkippedNumbersReportNumberGenetive,
                                                                Resources.SkippedNumbersReportNumberPlural);
        
        displayValue += string.Format(Sungero.Docflow.Reports.Resources.SkippedNumbersReport.And, hiddenSkippedNumberCount, numberLabel);
      }
      
      return displayValue;
    }
    
    /// <summary>
    /// Заполнить отчет данными о документах.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="args">Аргументы построения отчета.</param>
    public virtual void WriteToAvailableDocumentsTable(IQueryable<IOfficialDocument> documents,
                                                       Sungero.Docflow.Structures.SkippedNumbersReport.IBeforeExecuteArguments args)
    {
      var reportSessionId = args.ReportSessionId;
      
      var availableDocuments = new List<Structures.SkippedNumbersReport.AvailableDocument>();
      var previousDocDate = Calendar.SqlMinValue;
      foreach (var document in documents.ToList().OrderBy(x => x.Index))
      {
        var numberOnFormat = document.Index != null && document.Index != 0;
        var canRead = document.AccessRights.CanRead();
        var inCorrectOrder = (previousDocDate <= document.RegistrationDate || !numberOnFormat) &&
          (document.RegistrationDate >= args.PeriodBegin && document.RegistrationDate <= args.PeriodEnd);
        availableDocuments.Add(Structures.SkippedNumbersReport.AvailableDocument.Create(document.Id, numberOnFormat, canRead, inCorrectOrder, reportSessionId));
        
        if (numberOnFormat && inCorrectOrder)
          previousDocDate = document.RegistrationDate.Value;
      }
      
      SkippedNumbersReport.AvailableDocumentsTableName = Constants.SkippedNumbersReport.AvailableDocumentsTableName;
      Functions.Module.WriteStructuresToTable(SkippedNumbersReport.AvailableDocumentsTableName, availableDocuments);
    }
  }
}