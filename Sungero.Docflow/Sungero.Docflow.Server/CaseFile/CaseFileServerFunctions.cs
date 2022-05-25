using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.CaseFile;

namespace Sungero.Docflow.Server
{
  partial class CaseFileFunctions
  {
    /// <summary>
    /// Отображение документов в деле.
    /// </summary>
    /// <returns>Перечень документов в деле.</returns>
    [Remote]
    public IQueryable<IOfficialDocument> ShowCaseDocuments()
    {
      return OfficialDocuments.GetAll().Where(d => Equals(d.CaseFile, _obj));
    }
    
    /// <summary>
    /// Проверить индекс дела на уникальность в рамках нашей организации.
    /// </summary>
    /// <returns>True, если индекс уникален, иначе false.</returns>
    public virtual bool CheckIndexForUniqueness()
    {
      return this.CheckIndexForUniquenessInPeriod(_obj.StartDate, _obj.EndDate);
    }
    
    /// <summary>
    /// Проверить индекс дела на уникальность в рамках нашей организации.
    /// </summary>
    /// <param name="periodStart">Начало периода.</param>
    /// <param name="periodEnd">Конец периода.</param>
    /// <returns>True, если индекс уникален, иначе false.</returns>
    public virtual bool CheckIndexForUniquenessInPeriod(DateTime? periodStart, DateTime? periodEnd)
    {
      var originalStartDate = periodStart;
      var hasOriginalEndDate = periodEnd != null;
      var originalEndDate = hasOriginalEndDate ? periodEnd : Calendar.SqlMaxValue;
      var originalIndex = _obj.Index;
      var originalBusinessUnit = _obj.BusinessUnit;
      var originalDepartment = _obj.Department;
      var hasOriginalDepartment = originalDepartment != null;
      if (originalBusinessUnit == null && hasOriginalDepartment)
        originalBusinessUnit = Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(_obj.Department);
      var originalBusinessUnitDepartmentIds = new List<int>();
      if (originalBusinessUnit != null)
        originalBusinessUnitDepartmentIds = Company.PublicFunctions.BusinessUnit.Remote.GetAllDepartmentIds(originalBusinessUnit);
      
      return !CaseFiles.GetAll().Any(f => !Equals(f, _obj) &&
                                     f.Status.Value == CoreEntities.DatabookEntry.Status.Active &&
                                     f.Index == originalIndex &&
                                     (originalBusinessUnit == null && !hasOriginalDepartment && f.BusinessUnit == null && f.Department == null ||
                                      originalBusinessUnit == null && hasOriginalDepartment && f.BusinessUnit == null && Equals(originalDepartment, f.Department) ||
                                      originalBusinessUnit != null && f.BusinessUnit != null && Equals(originalBusinessUnit, f.BusinessUnit) ||
                                      originalBusinessUnit != null && f.BusinessUnit == null && f.Department != null && originalBusinessUnitDepartmentIds.Contains(f.Department.Id)) &&
                                     originalEndDate >= f.StartDate &&
                                     (f.EndDate != null && originalStartDate <= f.EndDate ||
                                      f.EndDate == null));
    }
    
    /// <summary>
    /// Копировать номенклатуру дел асинхронно.
    /// </summary>
    /// <param name="sourcePeriodStartDate">Начало исходного периода.</param>
    /// <param name="sourcePeriodEndDate">Конец исходного периода.</param>
    /// <param name="targetPeriodStartDate">Начало целевого периода.</param>
    /// <param name="targetPeriodEndDate">Конец целевого периода.</param>
    /// <param name="businessUnitId">ИД нашей организации.</param>
    /// <param name="departmentId">ИД подразделения.</param>
    [Public, Remote]
    public static void CopyCaseFilesAsync(DateTime sourcePeriodStartDate, DateTime sourcePeriodEndDate,
                                          DateTime targetPeriodStartDate, DateTime targetPeriodEndDate,
                                          int businessUnitId,
                                          int departmentId)
    {
      try
      {
        var copyCaseFilesAsync = Docflow.AsyncHandlers.CopyCaseFiles.Create();
        copyCaseFilesAsync.UserId = Users.Current.Id;
        copyCaseFilesAsync.SourcePeriodStartDate = sourcePeriodStartDate;
        copyCaseFilesAsync.SourcePeriodEndDate = sourcePeriodEndDate;
        copyCaseFilesAsync.TargetPeriodStartDate = targetPeriodStartDate;
        copyCaseFilesAsync.TargetPeriodEndDate = targetPeriodEndDate;
        copyCaseFilesAsync.BusinessUnitId = businessUnitId;
        copyCaseFilesAsync.DepartmentId = departmentId;
        copyCaseFilesAsync.ExecuteAsync();
      }
      catch (Exception ex)
      {
        var message = Resources.CaseFileCopyingErrorFormat(targetPeriodStartDate.Year);
        Functions.Module.SendStandardNotice(Resources.CheckCaseFileCopyingError, Users.Current, message, null, null);
        Logger.ErrorFormat("CopyCaseFilesAsync. Failed.", ex);
      }
    }
    
    /// <summary>
    /// Копировать номенклатуру дел.
    /// </summary>
    /// <param name="userId">ИД пользователя, инициировавшего копирование.</param>
    /// <param name="sourcePeriodStartDate">Начало исходного периода.</param>
    /// <param name="sourcePeriodEndDate">Конец исходного периода.</param>
    /// <param name="targetPeriodStartDate">Начало целевого периода.</param>
    /// <param name="targetPeriodEndDate">Конец целевого периода.</param>
    /// <param name="businessUnitId">ИД нашей организации.</param>
    /// <param name="departmentId">ИД подразделения.</param>
    [Public, Remote]
    public static void CopyCaseFiles(int userId,
                                     DateTime sourcePeriodStartDate, DateTime sourcePeriodEndDate,
                                     DateTime targetPeriodStartDate, DateTime targetPeriodEndDate,
                                     int businessUnitId, int departmentId)
    {
      Logger.DebugFormat("CopyCaseFiles. Start copy case files by user (Id {0}).", userId);
      Logger.DebugFormat("CopyCaseFiles. Source Period: {0} - {1}.", sourcePeriodStartDate, sourcePeriodEndDate);
      Logger.DebugFormat("CopyCaseFiles. Target Period: {0} - {1}.", targetPeriodStartDate, targetPeriodEndDate);
      Logger.DebugFormat("CopyCaseFiles. Business Unit Id: {0}.", businessUnitId);
      Logger.DebugFormat("CopyCaseFiles. Department Id: {0}.", departmentId);
      
      try
      {
        var success = 0;
        var failed = 0;
        var alreadyCopied = 0;
        var caseFiles = GetCaseFilesToCopy(sourcePeriodStartDate, sourcePeriodEndDate,
                                           businessUnitId, departmentId);
        Logger.DebugFormat("CopyCaseFiles. Case Files to copying: {0}.", caseFiles.Count());
        
        foreach (var caseFile in caseFiles)
        {
          var caseFileId = caseFile.Id;
          var isIndexUnique = Functions.CaseFile.CheckIndexForUniquenessInPeriod(caseFile,
                                                                                 targetPeriodStartDate,
                                                                                 targetPeriodEndDate);
          if (!isIndexUnique)
          {
            Logger.ErrorFormat("CopyCaseFiles. Case File (Id {0}). Index not unique in target period: {1}", caseFileId, caseFile.Index);
            alreadyCopied++;
            continue;
          }
          
          Transactions.Execute(() =>
                               {
                                 using (var session = new Sungero.Domain.Session())
                                 {
                                   try
                                   {
                                     Functions.CaseFile.CopyCaseFileOnNextPeriod(caseFile, targetPeriodStartDate, targetPeriodEndDate);
                                     session.SubmitChanges();
                                     success++;
                                     Logger.DebugFormat("CopyCaseFiles. Case File (Id {0}) success.", caseFileId);
                                   }
                                   catch (Exception ex)
                                   {
                                     failed++;
                                     Logger.ErrorFormat("CopyCaseFiles. Case File (Id {0}) failed.", ex, caseFileId);
                                   }
                                 }
                               });
          
        }
        
        Logger.DebugFormat("CopyCaseFiles. Total {0}, success {1}, failed {2}, already copied {3}.",
                           success + failed + alreadyCopied,
                           success,
                           failed,
                           alreadyCopied);
        
        var message = Functions.Module.GetCopyingCaseFilesTotalsMessage(targetPeriodStartDate, targetPeriodEndDate, success, failed);
        if (success + failed + alreadyCopied == 0)
          message = Functions.Module.GetNoCaseFilesToCopyMessage(targetPeriodStartDate, targetPeriodEndDate);
        // Номенклатура дел уже создана, если при копировании не создано ни одного нового дела.
        if (alreadyCopied > 0 && success == 0)
          message = Functions.Module.GetAlreadyCopiedCaseFilesMessage(targetPeriodStartDate, targetPeriodEndDate);
        
        Functions.Module.SendCopyCaseFilesNotification(userId, message);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("CopyCaseFiles. Error on copying case files.", ex);
      }
    }
    
    /// <summary>
    /// Получить дела для копирования.
    /// </summary>
    /// <param name="sourcePeriodStartDate">Начало исходного периода.</param>
    /// <param name="sourcePeriodEndDate">Конец исходного периода.</param>
    /// <param name="businessUnitId">ИД нашей организации.</param>
    /// <param name="departmentId">ИД подразделения.</param>
    /// <returns>Дела для копирования.</returns>
    public static List<ICaseFile> GetCaseFilesToCopy(DateTime sourcePeriodStartDate, DateTime sourcePeriodEndDate,
                                                     int businessUnitId, int departmentId)
    {
      var caseFiles = CaseFiles.GetAll()
        .Where(x => x.Status == Docflow.CaseFile.Status.Active &&
                    x.LongTerm != true &&
                    x.StartDate.Value >= sourcePeriodStartDate &&
                    x.EndDate.HasValue &&
                    x.EndDate.Value <= sourcePeriodEndDate);
      
      if (businessUnitId > 0)
        caseFiles = caseFiles.Where(x => x.BusinessUnit != null &&
                                         x.BusinessUnit.Id == businessUnitId);
      
      if (departmentId > 0)
      {
        var department = Sungero.Company.Departments.Get(departmentId);
        var subordinateDepartmentIds = Company.PublicFunctions.Department.Remote.GetSubordinateDepartmentIds(department);
        caseFiles = caseFiles.Where(x => x.Department != null &&
                                         (Equals(x.Department, department) ||
                                          subordinateDepartmentIds.Contains(x.Department.Id)));
      }
      
      return caseFiles.ToList();
    }
    
    /// <summary>
    /// Скопировать дело в заданный период.
    /// </summary>
    /// <param name="startDate">Дата начала.</param>
    /// <param name="endDate">Дата конца.</param>
    /// <returns>Дело.</returns>
    public virtual ICaseFile CopyCaseFileOnNextPeriod(DateTime startDate, DateTime endDate)
    {
      var newCaseFile = CaseFiles.Copy(_obj);
      newCaseFile.Note = Sungero.Docflow.CaseFiles.Resources.AutoCreatedCaseFileNote;
      Logger.DebugFormat("CopyCaseFiles: Case File (Id {0}) copying.", _obj.Id);
      
      // Dmitriev_IA: Даты начала и конца целевого периода копирования формируются программно
      //              в клиентской функции GetCaseFilesCopyDialogTargetPeriod() модуля Docflow
      //              и всегда принадлежат одному году.
      var periodYear = startDate.Year;
      var periodIsYear = startDate == Calendar.BeginningOfYear(startDate) &&
        endDate == Calendar.EndOfYear(startDate);
      if (periodIsYear)
      {
        newCaseFile.StartDate = newCaseFile.StartDate.Value.AddYears(periodYear - newCaseFile.StartDate.Value.Year);
        newCaseFile.EndDate = newCaseFile.EndDate.Value.AddYears(periodYear - newCaseFile.EndDate.Value.Year);
        
        Logger.DebugFormat("CopyCaseFiles: Case File Start Date changed {0} >> {1} ",
                           _obj.StartDate.Value.ToShortDateString(),
                           newCaseFile.StartDate.Value.ToShortDateString());
        Logger.DebugFormat("CopyCaseFiles: Case File End Date changed {0} >> {1} ",
                           _obj.EndDate.Value.ToShortDateString(),
                           newCaseFile.EndDate.Value.ToShortDateString());
      }
      
      newCaseFile.Save();
      return newCaseFile;
    }
  }
}